using System;
using System.Collections.Generic;
using System.Text;

namespace OsClasses
{
    public static class SJFpreemtive
    {
        private static readonly PriorityQueue<Process, (int primary, int secondary) > _processes = new PriorityQueue<Process, (int primary, int secondary)>();
        private static int _pid;
        private static int _count;

        public static async Task Run(PriorityQueue<Process, int> processes,bool isLiveMode = true)
        {
            _pid = 0;
            _count = 0;
            Process currentProcess = processes.Dequeue();
            while (processes.Count > 0 || _processes.Count > 0 || currentProcess != null)
            {
                if(currentProcess != null)
                    currentProcess.BurstTime--;
                if(isLiveMode)
                    await Task.Delay(1000);
                _count++;
                if (currentProcess != null && currentProcess.BurstTime == 0)
                {
                    currentProcess.FinishTime = _count;
                    currentProcess.WaitingTime += (currentProcess.FinishTime - currentProcess.ArrivalTime);
                    currentProcess.TurnaroundTime = currentProcess.FinishTime - currentProcess.ArrivalTime;
                    currentProcess = null;
                }
                while( processes.Count > 0 && processes.Peek().ArrivalTime == _count)
                {
                    if(currentProcess == null)
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
                    else if((processes.Peek().BurstTime < currentProcess.BurstTime ) && _processes.Count == 0)
                    {
                        _processes.Enqueue(currentProcess, (currentProcess.BurstTime, _pid++));
                        currentProcess = processes.Dequeue();
                    }
                    else if(processes.Peek().BurstTime < currentProcess.BurstTime && processes.Peek().BurstTime < _processes.Peek().BurstTime)
                    {
                        _processes.Enqueue(currentProcess, (currentProcess.BurstTime, _pid++));
                        currentProcess = processes.Dequeue();
                    }
                    else if(_processes.Peek().BurstTime < currentProcess.BurstTime && _processes.Peek().BurstTime < processes.Peek().BurstTime)
                    {   
                         currentProcess = _processes.Dequeue();
                        _processes.Enqueue(currentProcess, (currentProcess.BurstTime, _pid++));
                    }
                    else if (processes.Count > 0)
                    {
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
