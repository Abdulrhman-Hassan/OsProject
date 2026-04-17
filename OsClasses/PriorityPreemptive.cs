using OsClasses.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace OsClasses
{


    public static class PriorityPreemptive
    {
        private static int _pid;
        private static int _count;

        public static ObservableCollection<GanttBlock> Gantt = new ObservableCollection<GanttBlock>(); // For Gantt Chart GUI Part

        // Main priority queue: lower value = higher priority. Tie-breaker: arrival order (pid)
        private static readonly PriorityQueue<Process, (int priority, int pid)> _readyQueue = new PriorityQueue<Process, (int, int)>();

        private static readonly PriorityQueue<Process, int> _tieBreakerPQ = new PriorityQueue<Process, int>(); // will have the processes which have same priorities
        private static readonly Dictionary<Process, int> _originalPids = new Dictionary<Process, int>();

        public static void Reset()
        {
            Gantt.Clear();
            _readyQueue.Clear();
            _tieBreakerPQ.Clear();
            _originalPids.Clear();
            _pid = 0;
            _count = 0;
        }

        public static async Task Run(PriorityQueue<Process, int> processes, int quantum, bool isLiveMode = false)
        {
            await Run(processes, quantum, TieBreakerAlgorithm.FCFS, isLiveMode);
        }

        public static async Task Run(PriorityQueue<Process, int> processes, int quantum, TieBreakerAlgorithm tieBreaker, bool isLiveMode = false)
        {
            _pid = 0;
            _count = 0;

            int enqueueOrder = 0;
            Process currentProcess = null;
            bool isTieBreakerMode = false;
            int remainingQuantum = quantum;

            while (processes.Count > 0 || _readyQueue.Count > 0 || currentProcess != null || _tieBreakerPQ.Count > 0)
            {
                bool preemptionOccurred = false;

                // 1. Move arriving processes to the ready queue
                while (processes.Count > 0 && processes.Peek().ArrivalTime <= _count)
                {
                    var p = processes.Dequeue();
                    int assignedPid = _pid++;
                    _originalPids[p] = assignedPid;
                    _readyQueue.Enqueue(p, (p.Priority, assignedPid));

                    // Check for preemption
                    if (currentProcess != null)
                    {
                        if (p.Priority < currentProcess.Priority)
                        {
                            preemptionOccurred = true;
                        }
                        else if (p.Priority == currentProcess.Priority)
                        {
                            if (tieBreaker == TieBreakerAlgorithm.SJF && p.BurstTime < currentProcess.BurstTime)
                            {
                                preemptionOccurred = true;
                            }
                        }
                    }
                }

                // 2. Handle Preemption by a higher-priority process
                if (preemptionOccurred && currentProcess != null)
                {
                    currentProcess.IsExecuting = false;

                    // If we were in tie-breaker mode, put the current process back into _tieBreakerPQ
                    // and then drain the entire tie-breaker queue back into the main ready queue,
                    // since a higher-priority process has arrived and takes over.
                    if (isTieBreakerMode)
                    {
                        _tieBreakerPQ.Enqueue(currentProcess, enqueueOrder++);
                        while (_tieBreakerPQ.Count > 0)
                        {
                            var tp = _tieBreakerPQ.Dequeue();
                            _readyQueue.Enqueue(tp, (tp.Priority, _originalPids[tp]));
                        }
                        isTieBreakerMode = false;
                    }
                    else
                    {
                        _readyQueue.Enqueue(currentProcess, (currentProcess.Priority, _originalPids[currentProcess]));
                    }
                    currentProcess = null;
                }

                bool wasNull = currentProcess == null;

                // 3. Select process to run
                if (currentProcess == null)
                {
                    // If in tie-breaker mode, check if a higher or EQUAL-priority process has
                    // arrived in the main ready queue before continuing the tied group.
                    if (isTieBreakerMode && _tieBreakerPQ.Count > 0)
                    {
                        if (_readyQueue.Count > 0 && _readyQueue.Peek().Priority <= _tieBreakerPQ.Peek().Priority)
                        {
                            // New process arrived for this priority level (or higher) — exit tie-breaker mode to re-evaluate sorting!
                            while (_tieBreakerPQ.Count > 0)
                            {
                                var tp = _tieBreakerPQ.Dequeue();
                                _readyQueue.Enqueue(tp, (tp.Priority, _pid++));
                            }
                            isTieBreakerMode = false;
                        }
                        else
                        {
                            currentProcess = _tieBreakerPQ.Dequeue();
                            remainingQuantum = quantum;
                        }
                    }

                    // Normal selection (also handles the fall-through from tie-breaker exit above)
                    if (!isTieBreakerMode && currentProcess == null && _readyQueue.Count > 0)
                    {
                        var first = _readyQueue.Dequeue();

                        // Check for tie: another process with same priority
                        if (_readyQueue.Count > 0 && _readyQueue.Peek().Priority == first.Priority)
                        {
                            var tied = new List<Process> { first };
                            while (_readyQueue.Count > 0 && _readyQueue.Peek().Priority == first.Priority)
                                tied.Add(_readyQueue.Dequeue());

                            isTieBreakerMode = true;

                            switch (tieBreaker)
                            {
                                case TieBreakerAlgorithm.FCFS:
                                    foreach (var tp in tied) _tieBreakerPQ.Enqueue(tp, enqueueOrder++);
                                    remainingQuantum = -1; // run to completion
                                    break;

                                case TieBreakerAlgorithm.RoundRobin:
                                    foreach (var tp in tied) _tieBreakerPQ.Enqueue(tp, enqueueOrder++);
                                    remainingQuantum = quantum;
                                    break;

                                case TieBreakerAlgorithm.SJF:
                                    // Mini SJF: order by burst time (shortest first)
                                    foreach (var tp in tied) _tieBreakerPQ.Enqueue(tp, tp.BurstTime * 10000 + enqueueOrder++);
                                    remainingQuantum = -1; // run to completion
                                    break;
                            }

                            currentProcess = _tieBreakerPQ.Dequeue();
                        }
                        else
                        {
                            // No tie — run normally
                            currentProcess = first;
                            if (tieBreaker == TieBreakerAlgorithm.RoundRobin) remainingQuantum = quantum;
                        }
                    }
                }

                bool isNewContextSwitch = wasNull && currentProcess != null;

                // 4. Execute the current process
                if (currentProcess != null)
                {
                    currentProcess.IsExecuting = true;

                    // 5. Update Gantt Chart
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != currentProcess.Id || isNewContextSwitch)
                        Gantt.Add(new GanttBlock { Pid = currentProcess.Id, StartTime = _count, EndTime = _count + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = _count + 1;

                    currentProcess.BurstTime--;

                    if (isLiveMode)
                    {
                        await Task.Delay(1000);
                        while (SchedulerState.IsPaused && !SchedulerState.IsCancelled) await Task.Delay(100);
                        if (SchedulerState.IsCancelled) return;
                    }

                    _count++;

                    if (tieBreaker == TieBreakerAlgorithm.RoundRobin)
                        --remainingQuantum;

                    // 5. Check if finished
                    if (currentProcess.BurstTime == 0)
                    {
                        currentProcess.FinishTime = _count;
                        currentProcess.TurnaroundTime = currentProcess.FinishTime - currentProcess.ArrivalTime;
                        currentProcess.WaitingTime += (currentProcess.FinishTime - currentProcess.ArrivalTime);
                        currentProcess.IsExecuting = false;
                        currentProcess = null;

                        if (isTieBreakerMode && _tieBreakerPQ.Count == 0)
                            isTieBreakerMode = false;

                        continue;
                    }

                    // 6. RoundRobin tie-breaker quantum expired
                    if (tieBreaker == TieBreakerAlgorithm.RoundRobin && remainingQuantum == 0)
                    {
                        currentProcess.IsExecuting = false;

                        if (isTieBreakerMode)
                        {
                            _tieBreakerPQ.Enqueue(currentProcess, enqueueOrder++);
                        }
                        else
                        {
                            _originalPids[currentProcess] = _pid++;
                            _readyQueue.Enqueue(currentProcess, (currentProcess.Priority, _originalPids[currentProcess]));
                        }

                        currentProcess = null;
                    }
                }
                else
                {
                    // IDLE state
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != 0)
                        Gantt.Add(new GanttBlock { Pid = 0, StartTime = _count, EndTime = _count + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = _count + 1;

                    if (isLiveMode)
                    {
                        await Task.Delay(1000);
                        while (SchedulerState.IsPaused && !SchedulerState.IsCancelled) await Task.Delay(100);
                        if (SchedulerState.IsCancelled) return;
                    }
                    _count++;
                }
            }
        }
    }
}
