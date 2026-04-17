using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OsClasses;
using OsClasses.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace SchedulerGUI;

// ─── Algorithm descriptor ────────────────────────────────────────────────────

/// <summary>
/// Binds a display name to the static Run / Reset / Gantt members
/// of each algorithm class — no interface, just delegates.
/// </summary>
internal sealed record AlgorithmEntry(
    string DisplayName,
    Func<PriorityQueue<Process, int>, int, bool, Task> Run,
    Action Reset,
    Func<ObservableCollection<GanttBlock>> GetGantt,
    bool NeedsQuantum,
    bool NeedsPriority
);

// ─── Main window ─────────────────────────────────────────────────────────────

public sealed partial class MainWindow : Window
{
    public ObservableCollection<Process> ProcessesList { get; } = new();
    private int _pidCounter = 1;

    private AlgorithmEntry _active;
    private TieBreakerAlgorithm _selectedTieBreaker = TieBreakerAlgorithm.RoundRobin;

    // ── Live-mode state ──────────────────────────────────────────────────────
    // Holds a reference to the PriorityQueue the algorithm is actively consuming.
    // When Live Mode is on and a new process is added, it's enqueued here so the
    // running algorithm picks it up on its next tick.
    // Thread-safe: WinUI 3 uses a single UI thread; await Task.Delay in the
    // algorithms yields back to that same thread, so the PQ is never accessed
    // concurrently.
    private PriorityQueue<Process, int> _runningPQ;
    private Queue<Process> _runningFcfsQueue;  // FCFS uses Queue<Process>, not the PQ
    private bool _isRunning;
    private bool _liveMode;
    private int _enqueueCounter; // stable tiebreaker for live-injected processes

    public static bool IsPaused
    {
        get => OsClasses.SchedulerState.IsPaused;
        set => OsClasses.SchedulerState.IsPaused = value;
    }
    public static bool IsCancelled
    {
        get => OsClasses.SchedulerState.IsCancelled;
        set => OsClasses.SchedulerState.IsCancelled = value;
    }

    // ── All supported algorithms — add new static class entries here ──────
    private static readonly AlgorithmEntry[] Algorithms =
    [
        new("Priority Non-Preemptive",
            PriorityNonPreemptive.Run,
            PriorityNonPreemptive.Reset,
            () => PriorityNonPreemptive.Gantt,
            NeedsQuantum: true,
            NeedsPriority: true),

        new("Priority Preemptive",
            PriorityPreemptive.Run,
            PriorityPreemptive.Reset,
            () => PriorityPreemptive.Gantt,
            NeedsQuantum: true,
            NeedsPriority: true),

        new("Round Robin",
            (pq, quantum, live) =>
            {
                var q = new Queue<Process>();
                while (pq.Count > 0) q.Enqueue(pq.Dequeue());
                return RoundRobin.Run(q, live, quantum);
            },
            RoundRobin.Reset,
            () => RoundRobin.Gantt,
            NeedsQuantum: true,
            NeedsPriority: false),

        new("FCFS",
            // FCFS.Run takes a Queue<Process> (sorted by arrival), so we drain
            // the PriorityQueue (already ordered by arrival time) into a Queue first.
            (pq, quantum, live) =>
            {
                var q = new Queue<Process>();
                while (pq.Count > 0) q.Enqueue(pq.Dequeue());
                return FCFS.Run(q, live);
            },
            FCFS.Reset,
            () => FCFS.Gantt,
            NeedsQuantum: false,
            NeedsPriority: false),

        new("SJF Non-Preemptive",
            (pq, quantum, live) => SJFnonpreemtive.Run(pq, live),
            SJFnonpreemtive.Reset,
            () => SJFnonpreemtive.Gantt,
            NeedsQuantum: false,
            NeedsPriority: false),

        new("SJF Preemptive (SRTF)",
            SjfPreemptive.Run,
            SjfPreemptive.Reset,
            () => SjfPreemptive.Gantt,
            NeedsQuantum: false,
            NeedsPriority: false),
    ];

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");

        LvProcesses.ItemsSource = ProcessesList;

        // Populate the algorithm ComboBox
        foreach (var alg in Algorithms)
            CbAlgorithm.Items.Add(alg.DisplayName);

        // Populate the tie-breaker ComboBox with enum names
        foreach (var tb in Enum.GetValues(typeof(TieBreakerAlgorithm)))
            CbTieBreaker.Items.Add(tb.ToString());
        CbTieBreaker.SelectedIndex = 1; // default to RoundRobin

        CbAlgorithm.SelectedIndex = 0; // triggers OnAlgorithmChanged
    }

    // ── Algorithm selector ───────────────────────────────────────────────────

    private void OnAlgorithmChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CbAlgorithm.SelectedIndex < 0) return;

        _active = Algorithms[CbAlgorithm.SelectedIndex];

        // Point the Gantt chart at the newly selected algorithm's collection
        IcGantt.ItemsSource = _active.GetGantt();

        // Show / hide Priority input, header column, AND data column
        var needsPriority = _active.NeedsPriority;
        NbPriority.Visibility = needsPriority ? Visibility.Visible : Visibility.Collapsed;

        // Show / hide Tie Breaker ComboBox — only for Priority-based algorithms
        CbTieBreaker.Visibility = needsPriority ? Visibility.Visible : Visibility.Collapsed;

        // Show / hide Quantum:
        // For priority algorithms: show only when RoundRobin tie-breaker is selected
        // For non-priority algorithms: show if the algorithm needs it
        if (needsPriority)
            NbQuantum.Visibility = _selectedTieBreaker == TieBreakerAlgorithm.RoundRobin
                ? Visibility.Visible : Visibility.Collapsed;
        else
            NbQuantum.Visibility = _active.NeedsQuantum ? Visibility.Visible : Visibility.Collapsed;

        // Header column: set width to 0 to reclaim full space, or restore to 1.2*
        ColPriorityHeader.Width = needsPriority
            ? new Microsoft.UI.Xaml.GridLength(1.2, Microsoft.UI.Xaml.GridUnitType.Star)
            : new Microsoft.UI.Xaml.GridLength(0);

        // Switch DataTemplate so the data rows also gain/lose the Priority column
        // (ensures header and row column widths stay perfectly aligned)
        // In WinUI 3, Window has no Resources — access them via the root FrameworkElement (Content).
        var res = (FrameworkElement)Content;
        LvProcesses.ItemTemplate = needsPriority
            ? (Microsoft.UI.Xaml.DataTemplate)res.Resources["ProcTplWithPriority"]
            : (Microsoft.UI.Xaml.DataTemplate)res.Resources["ProcTplNoPriority"];

    }

    // ── Tie-breaker selector ─────────────────────────────────────────────────

    private void OnTieBreakerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CbTieBreaker.SelectedIndex < 0) return;

        _selectedTieBreaker = (TieBreakerAlgorithm)CbTieBreaker.SelectedIndex;

        // Quantum is only relevant when RoundRobin tie-breaker is selected
        if (_active != null && _active.NeedsPriority)
            NbQuantum.Visibility = _selectedTieBreaker == TieBreakerAlgorithm.RoundRobin
                ? Visibility.Visible : Visibility.Collapsed;

    }

    // ── Process management ───────────────────────────────────────────────────

    private TextBox GetInnerTextBox(DependencyObject obj)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is TextBox tb) return tb;
            var result = GetInnerTextBox(child);
            if (result != null) return result;
        }
        return null;
    }

    private async void OnAddProcessClick(object sender, RoutedEventArgs e)
    {
        // ── Input validation ────────────────────────────────────────────────
        string rawArrival = GetInnerTextBox(NbArrival)?.Text ?? NbArrival.Text;
        string rawBurst = GetInnerTextBox(NbBurst)?.Text ?? NbBurst.Text;
        string rawPriority = GetInnerTextBox(NbPriority)?.Text ?? NbPriority.Text;

        bool arrivalParsed = double.TryParse(rawArrival, out double arrivalVal);
        bool burstParsed = double.TryParse(rawBurst, out double burstVal);
        bool priorityParsed = double.TryParse(rawPriority, out double priorityVal);

        bool arrivalInvalid = !_isRunning && (!arrivalParsed || arrivalVal < 0);
        bool burstInvalid = !burstParsed || burstVal < 1;
        bool priorityInvalid = !priorityParsed || priorityVal < 1;

        if (arrivalInvalid || burstInvalid || priorityInvalid)
        {
            string msg = "";
            if (arrivalInvalid)
                msg += !arrivalParsed ? "• Arrival Time must be a valid number (characters are not allowed).\n" : "• Arrival Time must be a non-negative number.\n";
            if (burstInvalid)
                msg += !burstParsed ? "• Burst Time must be a valid number (characters are not allowed).\n" : "• Burst Time must be a positive number (≥ 1).\n";
            if (priorityInvalid)
                msg += !priorityParsed ? "• Priority must be a valid number (characters are not allowed).\n" : "• Priority must be a positive number (≥ 1).\n";

            var dialog = new ContentDialog
            {
                Title = "Invalid Input",
                Content = msg.TrimEnd(),
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
            };
            await dialog.ShowAsync();
            return;
        }

        int computedArrival = 0;
        if (_isRunning)
        {
            var gantt = _active?.GetGantt();
            computedArrival = (gantt != null && gantt.Count > 0) ? gantt[gantt.Count - 1].EndTime : 0;
        }
        else
        {
            computedArrival = (int)NbArrival.Value;
        }

        var p = new Process
        {
            Id = _pidCounter++,
            ArrivalTime = computedArrival,
            BurstTime = (int)NbBurst.Value,
            Priority = (byte)NbPriority.Value,
            WaitingTime = -(int)NbBurst.Value,   // match parameterized ctor: algorithms do += TurnaroundTime
        };
        ProcessesList.Add(p);

        // ── Live Mode: inject into the running algorithm's input queue ───────
        // The algorithm consumes _runningPQ via processes.Dequeue(). By enqueuing
        // the new process here (between algorithm ticks), it will be picked up
        // on the next iteration of the algorithm's while-loop.
        if (_isRunning && _liveMode)
        {
            if (_runningFcfsQueue != null)
                _runningFcfsQueue.Enqueue(p);          // FCFS path: inject into Queue
            else if (_runningPQ != null)
                _runningPQ.Enqueue(p, (p.ArrivalTime * 10000) + _enqueueCounter++);
        }

        NbArrival.Value = 0;
        NbBurst.Value = 1;
        NbPriority.Value = 1;
    }

    private void OnPauseClick(object sender, RoutedEventArgs e)
    {
        IsPaused = !IsPaused;
        BtnPause.Content = IsPaused ? "Resume" : "Pause";

        if (_liveMode)
        {
            SetInputControlsEnabled(IsPaused);
        }
    }

    private async void OnStartClick(object sender, RoutedEventArgs e)
    {
        if (ProcessesList.Count == 0)
        {
            var dialog = new ContentDialog
            {
                Title = "No Processes Found",
                Content = "You must add new processes to start the scheduler.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
            };
            await dialog.ShowAsync();
            return;
        }

        string rawQuantum = GetInnerTextBox(NbQuantum)?.Text ?? NbQuantum.Text;
        bool quantumParsed = double.TryParse(rawQuantum, out double quantumVal);
        if (_active.NeedsQuantum && (!quantumParsed || quantumVal < 1))
        {
            string msg = !quantumParsed
                ? "Quantum must be a valid number (characters are not allowed)."
                : "Quantum must be a positive number (≥ 1).";

            var dialog = new ContentDialog
            {
                Title = "Invalid Input",
                Content = msg,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
            };
            await dialog.ShowAsync();
            return;
        }

        // ── Disable Start to prevent multiple clicks ────────────────────────
        BtnStart.IsEnabled = false;

        // ── If Live Mode is OFF, lock the process-input controls ─────────────
        _liveMode = CbLiveMode.IsChecked == true;

        BtnPause.IsEnabled = _liveMode;
        BtnPause.Content = "Pause";
        IsPaused = false;
        IsCancelled = false;

        CbAlgorithm.IsEnabled = false;
        CbTieBreaker.IsEnabled = false;
        NbQuantum.IsEnabled = false;
        CbLiveMode.IsEnabled = false;

        NbArrival.Visibility = Visibility.Collapsed;

        SetInputControlsEnabled(false);

        // Build a PriorityQueue ordered by arrival time.
        // Use the list *index* (not p.Id) as the secondary key so that processes
        // added in insertion order are always dequeued in that same order when their
        // arrival times are identical — regardless of how IDs were assigned.
        var pq = new PriorityQueue<Process, int>();
        for (int i = 0; i < ProcessesList.Count; i++)
        {
            var p = ProcessesList[i];
            p.FinishTime = 0;
            p.TurnaroundTime = 0;
            p.WaitingTime = -p.BurstTime;  // algorithms do += TurnaroundTime, so must start at -BurstTime
            pq.Enqueue(p, (p.ArrivalTime * 10000) + i);  // i is stable insertion order
        }

        // ── Store the live PQ reference so OnAddProcessClick can inject ──────
        _runningPQ = pq;
        _enqueueCounter = ProcessesList.Count; // continue numbering after existing
        _isRunning = true;

        _active.Reset();
        IcGantt.ItemsSource = _active.GetGantt(); // re-bind after Reset clears the collection

        int quantum = (int)NbQuantum.Value;

        // FCFS special case: its Run() takes a Queue<Process>, not a PriorityQueue.
        // The adapter lambda in Algorithms[] drains pq into a local Queue, severing
        // the live connection.  Handle it here so we keep a Queue reference for
        // live-mode injection.
        if (_active.DisplayName == "FCFS")
        {
            var q = new Queue<Process>();
            while (pq.Count > 0) q.Enqueue(pq.Dequeue());
            _runningFcfsQueue = q;
            await FCFS.Run(q, _liveMode);
        }
        else if (_active.DisplayName == "Round Robin")
        {
            var q = new Queue<Process>();
            while (pq.Count > 0) q.Enqueue(pq.Dequeue());
            _runningFcfsQueue = q; // Reusing _runningFcfsQueue since both algorithms use a dynamically injected Queue
            await RoundRobin.Run(q, _liveMode, quantum);
        }
        else if (_active.NeedsPriority)
        {
            // Priority algorithms: pass the selected tie-breaker
            if (_active.DisplayName == "Priority Non-Preemptive")
                await PriorityNonPreemptive.Run(pq, quantum, _selectedTieBreaker, _liveMode);
            else
                await PriorityPreemptive.Run(pq, quantum, _selectedTieBreaker, _liveMode);
        }
        else
        {
            await _active.Run(pq, quantum, _liveMode);
        }

        // ── Run complete — tear down live state ─────────────────────────────
        _isRunning = false;
        _runningPQ = null;
        _runningFcfsQueue = null;
        IsPaused = false;
        BtnPause.Content = "Pause";
        BtnPause.IsEnabled = false;

        // ── Compute averages once the run is complete ───────────────────────
        if (ProcessesList.Count > 0)
        {
            double avgWaiting = 0;
            double avgTurnaround = 0;
            foreach (var p in ProcessesList)
            {
                avgWaiting += p.WaitingTime;
                avgTurnaround += p.TurnaroundTime;
            }
            avgWaiting /= ProcessesList.Count;
            avgTurnaround /= ProcessesList.Count;

            TbAvgWaiting.Text = $"{avgWaiting:F2} unit time";
            TbAvgTurnaround.Text = $"{avgTurnaround:F2} unit time";
        }

        // ── After completion: lock inputs until Reset ────────────────────────
        // Start stays disabled — only Reset can begin a new session.
        SetInputControlsEnabled(false);
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        ProcessesList.Clear();
        _pidCounter = 1;
        _active?.Reset();
        TbAvgWaiting.Text = "\u2014";
        TbAvgTurnaround.Text = "\u2014";

        // Clear live-mode state
        _isRunning = false;
        _runningPQ = null;
        _runningFcfsQueue = null;

        // Restore all controls to their default enabled state
        BtnStart.IsEnabled = true;
        BtnPause.IsEnabled = false;
        IsPaused = false;
        IsCancelled = true;
        BtnPause.Content = "Pause";

        CbAlgorithm.IsEnabled = true;
        CbTieBreaker.IsEnabled = true;
        NbQuantum.IsEnabled = true;
        CbLiveMode.IsEnabled = true;

        NbArrival.Visibility = Visibility.Visible;
        SetInputControlsEnabled(true);
    }

    // ── Helper: enable / disable the process-input controls ─────────────────

    private void SetInputControlsEnabled(bool enabled)
    {
        NbArrival.IsEnabled = enabled;
        NbBurst.IsEnabled = enabled;
        NbPriority.IsEnabled = enabled;
        BtnAddProcess.IsEnabled = enabled;
    }
}


