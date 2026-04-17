using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace OsClasses;

public class GanttBlock : INotifyPropertyChanged
{
    private int _endTime;

    public int Pid { get; set; }

    public string Name => Pid == 0 ? "IDLE" : $"P{Pid}";

    public int StartTime { get; set; }

    public int EndTime
    {
        get => _endTime;
        set
        {
            _endTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Duration));
            OnPropertyChanged(nameof(Width));
        }
    }

    public int Duration => EndTime - StartTime;

    public double Width => Duration * 40.0; // 40px width per time unit for visual scaling

    public Visibility ShowStartText => StartTime == 0 ? Visibility.Visible : Visibility.Collapsed;

    public SolidColorBrush BlockColor =>
        new SolidColorBrush(Pid == 0 ? ColorHelper.FromArgb(255, 255, 102, 102) : ColorHelper.FromArgb(255, 0, 120, 215)); // Accent Color

    public SolidColorBrush ObjectColor =>
        new SolidColorBrush(Colors.White);

    public Thickness BorderThick =>
        new Thickness(Pid == 0 ? 0 : 0);

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
