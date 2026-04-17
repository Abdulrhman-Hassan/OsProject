using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OsClasses
{
    public static class FCFS
    {
        private static int _time;
        private static List<Process> allProcesses = new List<Process>();
        public static double totalWaiting{
            get { 
                double totalWaiting = 0;
                foreach (var p in allProcesses)
                {
                    totalWaiting += p.WaitingTime;
                }
                return totalWaiting;
             }
        }
        public static double totalTurnaround{
            get { 
                double totalTurnaround = 0;
                foreach (var p in allProcesses)
                {
                    totalTurnaround += p.TurnaroundTime;
                }
                return totalTurnaround;
             }
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
                    if(currentProcess != null)
                    {
                        allProcesses.Add(currentProcess);
                    }
                }

                if (currentProcess != null)
                {
                    currentProcess.BurstTime--;

                    if (currentProcess.BurstTime == 0)
                    {
                        currentProcess.FinishTime = _time + 1;
                        currentProcess.TurnaroundTime = currentProcess.FinishTime - currentProcess.ArrivalTime;
                        currentProcess.WaitingTime += currentProcess.FinishTime - currentProcess.ArrivalTime;

                        currentProcess = null;
                    }
                }

                if (isLiveMode)
                    await Task.Delay(1000);

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