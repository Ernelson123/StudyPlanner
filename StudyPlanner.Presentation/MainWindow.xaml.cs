using StudyPlanner.Presentation.ViewModels;
using System.Windows;

namespace StudyPlanner.Presentation;

public partial class MainWindow : Window
{
    // Parameterless constructor for XAML designer
    public MainWindow() : this(null) { }

    // Constructor for DI injection
    public MainWindow(MainViewModel? viewModel)
    {
        InitializeComponent();

        System.Diagnostics.Debug.WriteLine($"Current Culture: {System.Globalization.CultureInfo.CurrentCulture.Name}");
        System.Diagnostics.Debug.WriteLine($"Current UI Culture: {System.Globalization.CultureInfo.CurrentUICulture.Name}");


        if (viewModel is not null)
        {
            DataContext = viewModel;
        }
    }
}
