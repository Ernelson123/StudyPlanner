using System.Windows;
using StudyPlanner.Presentation.ViewModels;

namespace StudyPlanner.Presentation;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// Acts as the root View for the application, hosting the MainViewModel.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Parameterless constructor required by the XAML Designer.
    /// Calls the main constructor with null to allow visual rendering in Visual Studio.
    /// </summary>
    public MainWindow() : this(null) { }

    /// <summary>
    /// Constructor used at runtime via Dependency Injection.
    /// Receives the MainViewModel instance and sets it as the DataContext.
    /// </summary>
    /// <param name="viewModel">The MainViewModel instance injected by the DI container.</param>
    public MainWindow(MainViewModel? viewModel)
    {
        InitializeComponent();

        // Set the ViewModel as the DataContext for data binding
        // If viewModel is null (designer mode), DataContext remains null, which is safe
        if (viewModel is not null)
        {
            DataContext = viewModel;
        }
    }

    /// <summary>
    /// Overrides the OnClosed method to ensure proper cleanup of resources.
    /// Disposes the ViewModel if it implements IDisposable, preventing memory leaks
    /// from event subscriptions (e.g., Timer events).
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        // Check if the DataContext (ViewModel) implements IDisposable
        if (DataContext is IDisposable disposableViewModel)
        {
            // Dispose of the ViewModel to unsubscribe from events and release resources
            disposableViewModel.Dispose();
        }

        // Call the base class implementation to complete the closing process
        base.OnClosed(e);
    }
}
