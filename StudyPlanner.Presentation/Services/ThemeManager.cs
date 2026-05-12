using System.Windows;

namespace StudyPlanner.Presentation.Services
{
    /// <summary>
    /// Centralized service for managing application themes (Light/Dark).
    /// Dynamically swaps ResourceDictionaries at runtime without requiring an application restart.
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// Applies the selected theme (Light or Dark) to the entire application.
        /// Executes on the UI Thread using Dispatcher to ensure safe modification of WPF resources.
        /// </summary>
        /// <param name="isDark">True to apply Dark theme, False for Light theme.</param>
        public static void ApplyTheme(bool isDark)
        {
            // Determine the URI of the target theme dictionary
            var uri = new Uri(isDark ? "/Themes/DarkTheme.xaml" : "/Themes/LightTheme.xaml", UriKind.Relative);
            var themeDict = new ResourceDictionary { Source = uri };

            // Invoke on UI Thread to avoid cross-thread access exceptions
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Find and remove any existing theme dictionary to prevent duplicates/memory leaks
                // StringComparison.Ordinal ensures consistent behavior across different system locales
                var oldTheme = System.Windows.Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme", StringComparison.Ordinal) == true);

                if (oldTheme != null)
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(oldTheme);
                }

                // Add the new theme dictionary to the application's resource collection
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(themeDict);
            });
        }
    }
}
