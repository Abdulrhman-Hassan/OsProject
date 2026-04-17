using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace OsClasses
{
    public static class SjfPreemptive
    {
        public static ObservableCollection<GanttBlock> Gantt = new ObservableCollection<GanttBlock>(); // For Gantt chart visualization
        private static readonly PriorityQueue<Process, (int primary, int secondary)> _processes = new PriorityQueue<Process, (int primary, int secondary)>();
        private static int _pid;
        private static int _count;

        public static void Reset()
        {
            Gantt.Clear();
            _processes.Clear();
            _pid = 0;
            _count = 0;
        }
        public static async Task Run(PriorityQueue<Process, int> processes, int quantum = 0, bool isLiveMode = true)
        {
            _pid = 0;
            _count = 0;
            Process currentProcess = processes.Dequeue();
            while (processes.Count > 0 || _processes.Count > 0 || currentProcess != null)
            {
                if (currentProcess != null)
                {
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != currentProcess.Id)
                        Gantt.Add(new GanttBlock { Pid = currentProcess.Id, StartTime = _count, EndTime = _count + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = _count + 1;
                    currentProcess.BurstTime--;
                    currentProcess.IsExecuting = true;
                }
                else
                {
                    // Track idle time gracefully
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != 0)
                        Gantt.Add(new GanttBlock { Pid = 0, StartTime = _count, EndTime = _count + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = _count + 1;
                }
                if (isLiveMode)
                {
                    await Task.Delay(1000);
                    while (SchedulerGUI.MainWindow.IsPaused && !SchedulerGUI.MainWindow.IsCancelled) await Task.Delay(100);
                    if (SchedulerGUI.MainWindow.IsCancelled) return;
                }
                _count++;
                if (currentProcess != null && currentProcess.BurstTime == 0)
                {
                    currentProcess.FinishTime = _count;
                    currentProcess.WaitingTime += (currentProcess.FinishTime - currentProcess.ArrivalTime);
                    currentProcess.TurnaroundTime = currentProcess.FinishTime - currentProcess.ArrivalTime;
                    currentProcess.IsExecuting = false;
                    currentProcess = null;
                }
                while (processes.Count > 0 && processes.Peek().ArrivalTime == _count)
                {
                    if (currentProcess == null)
                    {
                        if (processes.Count > 0 && _processes.Count > 0)
                        {
                            if (processes.Peek().BurstTime < _processes.Peek().BurstTime)
                            {
                                currentProcess = processes.Dequeue();
                            }
                            else
                            {
                                currentProcess = _processes.Dequeue();
                            }

                        }
                        else if (processes.Count > 0)
                        {
                            currentProcess = processes.Dequeue();
                        }
                        else if (_processes.Count > 0)
                        {
                            currentProcess = _processes.Dequeue();
                        }
                    }
                    else if ((processes.Peek().BurstTime < currentProcess.BurstTime) && _processes.Count == 0)
                    {
                        currentProcess.IsExecuting = false;
                        _processes.Enqueue(currentProcess, (currentProcess.BurstTime, _pid++));
                        currentProcess = processes.Dequeue();
                    }
                    else if (_processes.Count > 0 && processes.Peek().BurstTime < currentProcess.BurstTime && processes.Peek().BurstTime < _processes.Peek().BurstTime)
                    {
                        currentProcess.IsExecuting = false;
                        _processes.Enqueue(currentProcess, (currentProcess.BurstTime, _pid++));
                        currentProcess = processes.Dequeue();
                    }
                    else if (_processes.Count > 0 && _processes.Peek().BurstTime < currentProcess.BurstTime && _processes.Peek().BurstTime < processes.Peek().BurstTime)
                    {
                        currentProcess.IsExecuting = false;
                        currentProcess = _processes.Dequeue();
                        _processes.Enqueue(currentProcess, (currentProcess.BurstTime, _pid++));
                    }
                    else if (processes.Count > 0)
                    {
                        currentProcess.IsExecuting = false;
                        int burstTime = processes.Peek().BurstTime;
                        _processes.Enqueue(processes.Dequeue(), (burstTime, _pid++));
                    }
                }
                if (currentProcess == null)
                {
                    if (processes.Count > 0 && _processes.Count > 0)
                    {
                        if ((processes.Peek().BurstTime < _processes.Peek().BurstTime) && processes.Peek().ArrivalTime < _count)
                        {
                            currentProcess = processes.Dequeue();
                        }
                        else
                        {
                            currentProcess = _processes.Dequeue();
                        }
                    }
                    else if (processes.Count > 0 && processes.Peek().ArrivalTime < _count)
                    {
                        currentProcess = processes.Dequeue();
                    }
                    else if (_processes.Count > 0)
                    {
                        currentProcess = _processes.Dequeue();
                    }
                }
            }

        }
    }
}
