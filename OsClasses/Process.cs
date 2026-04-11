namespace OsClasses
{
    public class Process
    {
        private int _burstTime;
        private int _arrivalTime;
        private byte _priority;

        public Process(int _burstTime) 
        {
            this._burstTime = _burstTime;
        }
        public Process(int _burstTime, int _arrivalTime)
        {
            this._burstTime = _burstTime;
            this._arrivalTime = _arrivalTime;
        }
        public Process(int _burstTime, byte _priority)
        {
            this._burstTime = _burstTime;
            this._priority = _priority;
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
            set { _arrivalTime= value; }
        }

        // todo : build GetAverageWaitingTime method
        // todo : build GetAverageTurnaroundTime
    }
}
