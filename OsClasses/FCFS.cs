using OsClasses.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace OsClasses
{
    public static class FCFS
    {
        private static int _time;
        public static ObservableCollection<GanttBlock> Gantt = new ObservableCollection<GanttBlock>(); // For Gantt chart visualization
        private static List<Process> allProcesses = new List<Process>();
        public static double totalWaiting
        {
            get
            {
                double totalWaiting = 0;
                foreach (var p in allProcesses)
                {
                    totalWaiting += p.WaitingTime;
                }
                return totalWaiting;
            }
        }
        public static double totalTurnaround
        {
            get
            {
                double totalTurnaround = 0;
                foreach (var p in allProcesses)
                {
                    totalTurnaround += p.TurnaroundTime;
                }
                return totalTurnaround;
            }
        }
        public static void Reset()
        {
            Gantt.Clear();
            allProcesses.Clear();
            _time = 0;
        }
        public static async Task Run(Queue<Process> processes, bool isLiveMode = true)
        {
            allProcesses.Clear();
            _time = 0;

            Queue<Process> readyQueue = new Queue<Process>();

            Process currentProcess = null;

            while (processes.Count > 0 || readyQueue.Count > 0 || currentProcess != null)
            {
                int count = processes.Count;

                for (int i = 0; i < count; i++)
                {
                    var p = processes.Dequeue();

                    if (p.ArrivalTime <= _time)
                        readyQueue.Enqueue(p);
                    else
                        processes.Enqueue(p);
                }

                if (currentProcess == null && readyQueue.Count > 0)
                {
                    currentProcess = readyQueue.Dequeue();
                    if (currentProcess != null)
                    {
                        allProcesses.Add(currentProcess);
                    }
                }

                if (currentProcess != null)
                {
                    currentProcess.IsExecuting = true;
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != currentProcess.Id)
                        Gantt.Add(new GanttBlock { Pid = currentProcess.Id, StartTime = _time, EndTime = _time + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = _time + 1;
                    currentProcess.BurstTime--;

                    if (currentProcess.BurstTime == 0)
                    {
                        currentProcess.FinishTime = _time + 1;
                        currentProcess.TurnaroundTime = currentProcess.FinishTime - currentProcess.ArrivalTime;
                        currentProcess.WaitingTime += currentProcess.FinishTime - currentProcess.ArrivalTime;
                        currentProcess.IsExecuting = false;
                        currentProcess = null;
                    }
                }
                else
                {
                    // Idle time, no process is executing
                    // Track idle time gracefully
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != 0)
                        Gantt.Add(new GanttBlock { Pid = 0, StartTime = _time, EndTime = _time + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = _time + 1;
                }
                if (isLiveMode)
                {
                    await Task.Delay(1000);
                    while (SchedulerState.IsPaused && !SchedulerState.IsCancelled) await Task.Delay(100);
                    if (SchedulerState.IsCancelled) return;
                }
                _time++;
            }
        }

        public static double GetAverageWaitingTime()
        {
            if (allProcesses.Count == 0) return 0;

            double total = 0;
            foreach (var p in allProcesses)
            {
                total += p.WaitingTime;
            }

            return total / allProcesses.Count;
        }

        public static double GetAverageTurnaroundTime()
        {
            if (allProcesses.Count == 0) return 0;

            double total = 0;
            foreach (var p in allProcesses)
            {
                total += p.TurnaroundTime;
            }

            return total / allProcesses.Count;
        }
    }
}
