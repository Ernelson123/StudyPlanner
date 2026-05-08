using System.Windows;
using System.Linq;
using System;

namespace StudyPlanner.Presentation.Services
{
    /// <summary>
    /// Gerenciador centralizado de temas.
    /// Troca os ResourceDictionary da aplicação em runtime sem reiniciar.
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// Aplica o tema selecionado.
        /// Usa Dispatcher para garantir que a modificação de recursos ocorra na UI Thread.
        /// </summary>
        public static void ApplyTheme(bool isDark)
        {
            var uri = new Uri(isDark ? "/Themes/DarkTheme.xaml" : "/Themes/LightTheme.xaml", UriKind.Relative);
            var themeDict = new ResourceDictionary { Source = uri };

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Remove tema antigo se existir (evita duplicação de memória)
#pragma warning disable CA1307 // Specify StringComparison for clarity
                var oldTheme = System.Windows.Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme") == true);
#pragma warning restore CA1307 // Specify StringComparison for clarity

                if (oldTheme != null)
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(oldTheme);
                // Adiciona o novo tema
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(themeDict);
            });
        }
    }
}
