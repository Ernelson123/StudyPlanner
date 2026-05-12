using System;
using System.Globalization;
using System.Windows.Data;

namespace StudyPlanner.Presentation.Converters
{
    /// <summary>
    /// Converts numeric hours (double/int/decimal) to a short string like "3.5h".
    /// Implements IValueConverter so it can be used from XAML as a StaticResource.
    /// </summary>
    public class HoursToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "0.0h";

#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                double hours = System.Convert.ToDouble(value, culture ?? CultureInfo.InvariantCulture);
                return string.Format(culture ?? CultureInfo.InvariantCulture, "{0:0.0}h", hours);
            }
            catch
            {
                return "0.0h";
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("HoursToStringConverter does not support ConvertBack.");
        }
    }
}
