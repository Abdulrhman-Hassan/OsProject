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

    public static class PriorityPreemptive
    {
        private static int _pid;
        private static int _count;

        public static ObservableCollection<GanttBlock> Gantt = new ObservableCollection<GanttBlock>(); // For Gantt Chart GUI Part

        // Main priority queue: lower value = higher priority. Tie-breaker: arrival order (pid)
        private static readonly PriorityQueue<Process, (int priority, int pid)> _readyQueue = new PriorityQueue<Process, (int, int)>();

        public static void Reset()
        {
            Gantt.Clear();
            _readyQueue.Clear();
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

            Process currentProcess = null;

            // Note: TieBreakerAlgorithm logic in preemptive mode usually defaults to FCFS for same priority.
            // In a truly preemptive system, if a new process arrives with SAME priority, it usually doesn't preempt.
            // If it has HIGHER priority (lower value), it preempts immediately.

            while (processes.Count > 0 || _readyQueue.Count > 0 || currentProcess != null)
            {
                bool preemptionOccurred = false;

                // 1. Move arriving processes to the ready queue
                while (processes.Count > 0 && processes.Peek().ArrivalTime <= _count)
                {
                    var p = processes.Dequeue();
                    _readyQueue.Enqueue(p, (p.Priority, _pid++));
                    
                    // Check for preemption: if new process has higher priority than current
                    if (currentProcess != null && p.Priority < currentProcess.Priority)
                    {
                        preemptionOccurred = true;
                    }
                }

                // 2. Handle Preemption
                if (preemptionOccurred && currentProcess != null)
                {
                    currentProcess.IsExecuting = false;
                    // Put current process back into ready queue. 
                    // To maintain its original arrival order relative to others of same priority, we'd need its original _pid.
                    // For simplicity, we re-enqueue it. In many implementations, preempted process goes to front or keeps position.
                    _readyQueue.Enqueue(currentProcess, (currentProcess.Priority, _pid++)); 
                    currentProcess = null;
                }

                // 3. Select process to run if idle
                if (currentProcess == null && _readyQueue.Count > 0)
                {
                    currentProcess = _readyQueue.Dequeue();
                }

                // 4. Execute one time unit
                if (currentProcess != null)
                {
                    currentProcess.IsExecuting = true;

                    // Update Gantt Chart
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != currentProcess.Id)
                        Gantt.Add(new GanttBlock { Pid = currentProcess.Id, StartTime = _count, EndTime = _count + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = _count + 1;

                    currentProcess.BurstTime--;

                    if (isLiveMode) await Task.Delay(1000);

                    _count++;

                    // 5. Check if finished
                    if (currentProcess.BurstTime == 0)
                    {
                        currentProcess.FinishTime = _count;
                        currentProcess.TurnaroundTime = currentProcess.FinishTime - currentProcess.ArrivalTime;
                        currentProcess.WaitingTime = currentProcess.TurnaroundTime - currentProcess.InitialBurstTime; // Corrected waiting time formula
                        currentProcess.IsExecuting = false;
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

                    _count++;
                }
            }
        }
    }
}
