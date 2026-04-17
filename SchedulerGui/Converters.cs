using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace SchedulerGUI
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts int values to display strings for process result columns.
    /// 
    /// Two modes controlled by ConverterParameter:
    ///   • Default (no parameter): "—" when value == 0; shows the number otherwise.
    ///     Used for FinishTime and TurnaroundTime, which are always > 0 once computed.
    ///   • "AllowZero": "—" only when value &lt; 0; shows 0 and positive numbers.
    ///     Used for WaitingTime, where 0 is a valid result (process never waited)
    ///     and the pre-computation state is negative (-BurstTime).
    /// </summary>
    public class ZeroToDashConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int i)
            {
                bool allowZero = parameter is string s
                    && s.Equals("AllowZero", StringComparison.OrdinalIgnoreCase);

                if (allowZero)
                    return i < 0 ? "\u2014" : i.ToString();   // —  for negative (pre-computation)
                else
                    return i == 0 ? "\u2014" : i.ToString();  // —  for zero     (not yet set)
            }
            return "\u2014";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
