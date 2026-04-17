using OsClasses.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace OsClasses
{
    public enum TieBreakerAlgorithm
    {
        FCFS,
        RoundRobin,
        SJF
    }

    public static class PriorityNonPreemptive
    {
        private static int _pid;
        private static int _count;

        public static ObservableCollection<GanttBlock> Gantt = new ObservableCollection<GanttBlock>(); // For Gantt Chart GUI Part

        private static readonly PriorityQueue<Process, (int priority, int pid)> _processes = new PriorityQueue<Process, (int, int)>();

        private static readonly PriorityQueue<Process, int> _tieBreakerPQ = new PriorityQueue<Process, int>(); // will have the processes which have same priorities
        private static readonly Dictionary<Process, int> _originalPids = new Dictionary<Process, int>();

        public static void Reset()
        {
            Gantt.Clear();
            _processes.Clear();
            _tieBreakerPQ.Clear();
            _originalPids.Clear();
            _pid = 0;
            _count = 0;
        }

        public static async Task Run(PriorityQueue<Process, int> processes, int quantum, bool isLiveMode = false)
        {
            await Run(processes, quantum, TieBreakerAlgorithm.FCFS, isLiveMode); // I found out that overloading is better due to implementaion that i made in MainWindow.xaml.cs
        }

        public static async Task Run(PriorityQueue<Process, int> processes, int quantum, TieBreakerAlgorithm tieBreaker, bool isLiveMode = false)
        {
            _pid = 0;
            _count = 0;

            int enqueueOrder = 0; // it will benifit us for ordering in the new queue to preserve the concept of FIFO or burst time depends on Tie breaker Algo
            Process currentProcess = null;
            bool isTieBreakerMode = false;
            int remainingQuantum = quantum;

            while (processes.Count > 0 || _processes.Count > 0 || currentProcess != null || _tieBreakerPQ.Count > 0)
            {
                // Preparing the Processes to begin simulation
                while (processes.Count > 0 && processes.Peek().ArrivalTime <= _count)
                {
                    var p = processes.Dequeue();
                    int assignedPid = _pid++;
                    _originalPids[p] = assignedPid;
                    _processes.Enqueue(p, (p.Priority, assignedPid));
                }

                bool wasNull = currentProcess == null;

                if (currentProcess == null)
                {
                    // Check if new process arrived that warrants re-evaluating the tie-breaker
                    if (isTieBreakerMode && _tieBreakerPQ.Count > 0)
                    {
                        if (_processes.Count > 0 && _processes.Peek().Priority <= _tieBreakerPQ.Peek().Priority)
                        {
                            while (_tieBreakerPQ.Count > 0)
                            {
                                var tp = _tieBreakerPQ.Dequeue();
                                _processes.Enqueue(tp, (tp.Priority, _originalPids[tp]));
                            }
                            isTieBreakerMode = false;
                        }
                        else
                        {
                            currentProcess = _tieBreakerPQ.Dequeue();
                            remainingQuantum = quantum;
                        }
                    }

                    if (!isTieBreakerMode && _processes.Count > 0)
                    {
                        var first = _processes.Dequeue(); // Getting first Process

                        // Check for tie: another process with same priority
                        if (_processes.Count > 0 && _processes.Peek().Priority == first.Priority)
                        {
                            var tied = new List<Process> { first };
                            while (_processes.Count > 0 && _processes.Peek().Priority == first.Priority)
                                tied.Add(_processes.Dequeue()); // Collecting it into a List

                            isTieBreakerMode = true;

                            switch (tieBreaker)
                            {
                                case TieBreakerAlgorithm.FCFS:
                                    foreach (var tp in tied) _tieBreakerPQ.Enqueue(tp, _originalPids[tp]);
                                    remainingQuantum = -1; // run to completion
                                    break;

                                case TieBreakerAlgorithm.RoundRobin:
                                    foreach (var tp in tied) _tieBreakerPQ.Enqueue(tp, _originalPids[tp]);
                                    remainingQuantum = quantum;
                                    break;

                                case TieBreakerAlgorithm.SJF:
                                    // Mini SJF: just order by burst time (shortest first)
                                    foreach (var tp in tied) _tieBreakerPQ.Enqueue(tp, tp.BurstTime * 10000 + _originalPids[tp]);
                                    remainingQuantum = -1; // run to completion
                                    break;
                            }

                            currentProcess = _tieBreakerPQ.Dequeue();
                        }
                        else
                        {
                            // if there is no Tie then work as normal
                            currentProcess = first;
                        }
                    }
                }

                bool isNewContextSwitch = wasNull && currentProcess != null;

                if (currentProcess != null)
                {
                    currentProcess.IsExecuting = true; // a flag for Highlight rows (GUI Part)


                    // GUI Part
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

                    if (isTieBreakerMode && tieBreaker == TieBreakerAlgorithm.RoundRobin)
                        --remainingQuantum; // it happens if it's on Round-Robin mode only

                    if (currentProcess.BurstTime == 0)
                    {
                        currentProcess.FinishTime = _count;
                        currentProcess.TurnaroundTime = currentProcess.FinishTime - currentProcess.ArrivalTime;
                        currentProcess.WaitingTime += (currentProcess.FinishTime - currentProcess.ArrivalTime);
                        currentProcess.IsExecuting = false;
                        currentProcess = null;

                        if (isTieBreakerMode && _tieBreakerPQ.Count == 0)
                            isTieBreakerMode = false;

                        continue; // Important to avoid the stuck for processes
                    }

                    if (isTieBreakerMode && tieBreaker == TieBreakerAlgorithm.RoundRobin && remainingQuantum == 0)
                    {
                        currentProcess.IsExecuting = false;
                        _tieBreakerPQ.Enqueue(currentProcess, enqueueOrder++);
                        currentProcess = null;
                    }
                }
                else
                {
                    // IDLE state (GUI PART)
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
