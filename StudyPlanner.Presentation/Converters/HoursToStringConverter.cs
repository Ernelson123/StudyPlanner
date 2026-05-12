using System;
using System.Globalization;
using System.Windows.Data;

namespace StudyPlanner.Presentation.Converters
{

    public class HoursToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double hours)
            {
                return $"{hours:F1}h";
            }
            return "0.0h";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
