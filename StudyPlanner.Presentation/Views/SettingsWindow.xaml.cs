using System.Windows;
using StudyPlanner.Presentation.ViewModels;

namespace StudyPlanner.Presentation.Views
{

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml.
    /// Acts as the View for the SettingsViewModel, handling UI-specific actions like closing the window.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// Constructor receives the SettingsViewModel via Dependency Injection.
        /// Sets the DataContext and subscribes to the ViewModel's close request event.
        /// </summary>
        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();

            // Set the ViewModel as the DataContext for data binding
            DataContext = viewModel;

            // Subscribe to the RequestClose event raised by the ViewModel.
            // When the ViewModel requests a close (e.g., after saving), this lambda executes Window.Close().
            // This keeps the ViewModel decoupled from the WPF Window class.
#pragma warning disable CA1062 // Validate arguments of public methods
            viewModel.RequestClose += () => this.Close();
#pragma warning restore CA1062 // Validate arguments of public methods
        }
    }
}
