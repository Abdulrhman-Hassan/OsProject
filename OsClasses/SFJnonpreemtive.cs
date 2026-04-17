using OsClasses.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
namespace OsClasses
{
    public static class SJFnonpreemtive
    {
        private static int Time;
        private static List<Process> allProcesses = new List<Process>();
        private static Dictionary<Process, int> originalBurstTimes = new Dictionary<Process, int>();
        public static ObservableCollection<GanttBlock> Gantt = new ObservableCollection<GanttBlock>(); // For Gantt chart visualization

        public static void Reset()
        {
            Gantt.Clear();
            allProcesses.Clear();
            originalBurstTimes.Clear();
            Time = 0;
        }
        public static async Task Run(PriorityQueue<Process, int> processes, bool isLiveMode = true)
        {
            Time = 0;
            allProcesses.Clear();
            originalBurstTimes.Clear();
            List<Process> readyQueue = new List<Process>();
            Process currentProcess = null;
            while (processes.Count > 0 || readyQueue.Count > 0 || currentProcess != null)
            {
                while (processes.Count > 0 && processes.Peek().ArrivalTime <= Time)
                {
                    Process p = processes.Dequeue();
                    readyQueue.Add(p);

                    if (!originalBurstTimes.ContainsKey(p))
                        originalBurstTimes[p] = p.BurstTime;
                }
                if (currentProcess == null && readyQueue.Count > 0)
                {
                    currentProcess = readyQueue[0];
                    foreach (var p in readyQueue)
                    {
                        if (originalBurstTimes[p] < originalBurstTimes[currentProcess])
                        {
                            currentProcess = p;

                        }
                    }
                    allProcesses.Add(currentProcess);
                    readyQueue.Remove(currentProcess);
                }
                if (currentProcess != null)
                {
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != currentProcess.Id)
                        Gantt.Add(new GanttBlock { Pid = currentProcess.Id, StartTime = Time, EndTime = Time + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = Time + 1;
                    currentProcess.BurstTime--;
                    currentProcess.IsExecuting = true;
                    if (currentProcess.BurstTime == 0)
                    {
                        currentProcess.FinishTime = Time + 1;
                        currentProcess.WaitingTime = currentProcess.FinishTime - currentProcess.ArrivalTime - originalBurstTimes[currentProcess];
                        currentProcess.TurnaroundTime = currentProcess.FinishTime - currentProcess.ArrivalTime;
                        currentProcess.IsExecuting = false;
                        currentProcess = null;
                    }
                }
                else
                {
                    // Track idle time gracefully
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != 0)
                        Gantt.Add(new GanttBlock { Pid = 0, StartTime = Time, EndTime = Time + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = Time + 1;
                }
                Time++;

                if (isLiveMode)
                {
                    await Task.Delay(1000);
                    while (SchedulerState.IsPaused && !SchedulerState.IsCancelled) await Task.Delay(100);
                    if (SchedulerState.IsCancelled) return;
                }
            }

        }
    }
}
