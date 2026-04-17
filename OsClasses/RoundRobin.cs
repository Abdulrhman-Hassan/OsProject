using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace OsClasses
{
    public static class RoundRobin
    {
        private static int _time;
        private static Queue<Process> _processes = new Queue<Process>();
        private static Dictionary<Process, int> _originalBurst = new Dictionary<Process, int>();

        public static ObservableCollection<GanttBlock> Gantt = new ObservableCollection<GanttBlock>();

        public static void Reset()
        {
            Gantt.Clear();
            _processes.Clear();
            _originalBurst.Clear();
            _time = 0;
        }

        public static async Task Run(Queue<Process> processes, bool isLiveMode = true, int Time_Quantum = 2)
        {
            _time = 0;
            _processes.Clear();
            _originalBurst.Clear();

            foreach (var p in processes)
            {
                _originalBurst[p] = p.BurstTime;
            }

            while (processes.Count > 0)
            {
                var process = processes.Dequeue();

                if (!_originalBurst.ContainsKey(process))
                    _originalBurst[process] = process.BurstTime;

                if (process.ArrivalTime > _time)
                {
                    int nextArrival = process.ArrivalTime;

                    foreach (var p in processes)
                    {
                        if (p.ArrivalTime < nextArrival)
                            nextArrival = p.ArrivalTime;
                    }

                    int idleTime = nextArrival - _time;

                    for (int i = 0; i < idleTime; i++)
                    {
                        if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != 0)
                            Gantt.Add(new GanttBlock { Pid = 0, StartTime = _time, EndTime = _time + 1 });
                        else
                            Gantt[Gantt.Count - 1].EndTime = _time + 1;

                        _time++;
                        if (isLiveMode)
                        {
                            await Task.Delay(1000);
                            while (SchedulerGUI.MainWindow.IsPaused) await Task.Delay(100);
                            if (!SchedulerGUI.MainWindow.IsPaused)
                            {
                                i = idleTime;
                            }
                        }
                    }

                    processes.Enqueue(process);
                    continue;
                }

                int runTime = Math.Min(Time_Quantum, process.BurstTime);
                process.IsExecuting = true;

                for (int i = 0; i < runTime; i++)
                {
                    process.BurstTime--;
                    if (Gantt.Count == 0 || Gantt[Gantt.Count - 1].Pid != process.Id)
                        Gantt.Add(new GanttBlock { Pid = process.Id, StartTime = _time, EndTime = _time + 1 });
                    else
                        Gantt[Gantt.Count - 1].EndTime = _time + 1;

                    _time++;
                    if (isLiveMode)
                    {
                        await Task.Delay(1000);
                        while (SchedulerGUI.MainWindow.IsPaused) await Task.Delay(100);
                    }
                }

                process.IsExecuting = false;

                if (process.BurstTime > 0)
                {
                    processes.Enqueue(process);
                }
                else
                {
                    process.FinishTime = _time;
                    process.TurnaroundTime = _time - process.ArrivalTime;
                    process.WaitingTime = process.TurnaroundTime - _originalBurst[process];
                    _processes.Enqueue(process);
                }
            }
            if (isLiveMode)
            {
                await Task.Delay(1000);
                while (SchedulerGUI.MainWindow.IsPaused) await Task.Delay(100);
            }
        }

        public static double GetAverageWaitingTime()
        {
            if (_processes.Count == 0) return 0;
            double total = 0;
            foreach (var p in _processes)
            {
                total += p.WaitingTime;
            }
            return total / _processes.Count;
        }

        public static double GetAverageTurnaroundTime()
        {
            if (_processes.Count == 0) return 0;
            double total = 0;
            foreach (var p in _processes)
            {
                total += p.TurnaroundTime;
            }
            return total / _processes.Count;
        }
    }
}
