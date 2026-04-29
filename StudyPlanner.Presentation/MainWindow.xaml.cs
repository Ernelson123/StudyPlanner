using StudyPlanner.Presentation.ViewModels;
using System.Windows;

namespace StudyPlanner.Presentation;

public partial class MainWindow : Window
{
    // Parameterless constructor for XAML designer
    public MainWindow() : this(null) { }

    // Constructor for Dependency Injection at runtime
    public MainWindow(MainViewModel? viewModel)
    {
        InitializeComponent();
        if (viewModel is not null)
        {
            DataContext = viewModel;
        }
    }

    // Clean up resources when window closes
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable vm)
        {
            vm.Dispose();
        }
        base.OnClosed(e);
    }
}
