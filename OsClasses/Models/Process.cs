using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OsClasses.Models
{
    public class Process : INotifyPropertyChanged
    {
        private int _id;
        private bool _isExecuting;
        private int _burstTime;
        private int _arrivalTime;
        private byte _priority;
        private int _finishTime;
        private int _waitingTime;
        private int _turnaroundTime;
        public string Name => $"P{Id}";
        public Process() { }
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
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(Name)); }
        }
        public bool IsExecuting
        {
            get => _isExecuting;
            set { _isExecuting = value; OnPropertyChanged(); }
        }
        public int BurstTime
        {
            get { return _burstTime; }
            set { _burstTime = value; OnPropertyChanged(); }
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
            set { _finishTime = value; OnPropertyChanged(); }
        }
        public int WaitingTime
        {
            get { return _waitingTime; }
            set { _waitingTime = value; OnPropertyChanged(); }
        }
        public int TurnaroundTime
        {
            get { return _turnaroundTime; }
            set { _turnaroundTime = value; OnPropertyChanged(); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
