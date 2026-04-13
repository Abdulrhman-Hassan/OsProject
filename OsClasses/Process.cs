namespace OsClasses
{
    public class Process
    {
        private int _burstTime;
        private int _arrivalTime;
        private byte _priority;
        private int _finishTime;
        private int _waitingTime;
        private int _turnaroundTime;

        public Process(int _burstTime) 
        {
            this._burstTime = _burstTime;
            _waitingTime = -_burstTime;
        }
        public Process(int _burstTime, int _arrivalTime)
        {
            this._burstTime = _burstTime;
            this._arrivalTime = _arrivalTime;
            _waitingTime = -_burstTime;
        }
        public Process(int _burstTime, byte _priority)
        {
            this._burstTime = _burstTime;
            this._priority = _priority;
            _waitingTime = -_burstTime;
        }
        public int BurstTime
        {
            get { return _burstTime; }
            set { _burstTime = value; }
        }
        public int ArrivalTime
        {
            get { return _arrivalTime; }
            set { _arrivalTime = value; }
        }
        public byte Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }
        public int FinishTime
        {
            get { return _finishTime; }
            set { _finishTime = value; }
        }
        public int WaitingTime
        {
            get { return _waitingTime; }
            set { _waitingTime = value; }
        }
        public int TurnaroundTime
        {
            get { return _turnaroundTime; }
            set { _turnaroundTime = value; }
        }

        // todo : build GetAverageWaitingTime method
        // todo : build GetAverageTurnaroundTime
    }
}
