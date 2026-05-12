using System;
using System.Globalization;
using System.Windows.Data;

namespace StudyPlanner.Presentation.Converters
{
    /// <summary>
    /// Converte bool para texto amigável no botão de tema.
    /// true -> "🌙 Dark" | false -> "️ Light"
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? " Dark" : "☀️ Light";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
