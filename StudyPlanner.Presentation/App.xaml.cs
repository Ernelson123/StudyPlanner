using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Services;
using StudyPlanner.Data;
using StudyPlanner.Data.Repositories;
using StudyPlanner.Data.Services;
using StudyPlanner.Presentation.Services;
using StudyPlanner.Presentation.ViewModels;
using System.Globalization;
using System.Windows;

namespace StudyPlanner.Presentation;

/// <summary>
/// Application entry point for the Study Planner WPF application.
/// This class handles:
/// - Application startup and shutdown
/// - Dependency Injection configuration
/// - Global error handling
/// - Logging setup
/// </summary>
public partial class App : System.Windows.Application
{
    /// <summary>
    /// Gets the application host (DI container).
    /// Static property allows access from anywhere in the app if needed.
    /// </summary>
    public static IHost? AppHost { get; private set; }

    /// <summary>
    /// Called when the application starts.
    /// This is the first code that runs after the app launches.
    /// </summary>
    /// <param name="e">Startup event arguments</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // ========== SET CULTURE ==========
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            // ========== APPLY DEFAULT THEME (Light) ==========
            StudyPlanner.Presentation.Services.ThemeManager.ApplyTheme(false);


            // ========== STEP 2: BUILD APPLICATION HOST ==========
            // Host.CreateDefaultBuilder() creates a standard .NET Generic Host
            // This provides:
            // - Dependency Injection container
            // - Logging framework
            // - Configuration system
            // - Lifetime management
            AppHost = Host.CreateDefaultBuilder()
                // Configure logging services
                .ConfigureLogging(logging =>
                {
                    // Clear default providers (we want custom config)
                    logging.ClearProviders();

                    // Add console logging (visible in Output window during debug)
                    logging.AddConsole();

                    // Set minimum log level to Information
                    // This filters out verbose Debug/Trace logs
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                // Configure Dependency Injection container
                .ConfigureServices((_, services) =>
                {
                    // ========== REGISTER APPLICATION SERVICES ==========
                    // These are singleton services (one instance for entire app lifetime)

                    // Timer service manages the Pomodoro countdown
                    // Singleton because we want one global timer
                    services.AddSingleton<ITimerService, PomodoroTimerService>();

                    // Time allocator calculates study schedules
                    services.AddSingleton<ITimeAllocator, TimeAllocator>();

                    // Notification service shows desktop notifications
                    services.AddSingleton<INotificationService, DesktopNotificationService>();

                    // ========== REGISTER DOMAIN SERVICES ==========
                    // These are scoped services (new instance per "scope")
                    // In WPF, we use Transient instead of Scoped

                    // Planning service handles study planning logic
                    services.AddScoped<IPlanningService, PlanningService>();

                    // Statistics service calculates study statistics
                    services.AddScoped<IStatisticsService, StatisticsService>();

                    // ========== REGISTER DATA LAYER ==========

                    // Configure Entity Framework Core with SQLite database
                    // This registers StudyPlannerDbContext with DI container
                    services.AddDbContext<StudyPlannerDbContext>(options =>
                        options.UseSqlite("Data Source=studyplanner.db"));

                    // Register repositories (data access layer)
                    // Transient means new instance each time (lightweight)
                    services.AddTransient<ISubjectRepository, SubjectRepository>();

                    // Register study session service
                    // This handles recording and retrieving study sessions
                    services.AddTransient<IStudySessionService, StudySessionService>();

                    // ========== REGISTER UI LAYER ==========

                    // Register MainWindow - created once when app starts
                    services.AddTransient<MainWindow>();

                    // Register MainViewModel - created with MainWindow
                    // ViewModels are transient (new instance per view)
                    services.AddTransient<MainViewModel>();
                })
                // Build the host (finalize configuration)
                .Build();

            // ========== STEP 3: SUBSCRIBE TO GLOBAL ERROR HANDLERS ==========
            // These catch exceptions that aren't handled elsewhere
            // Prevents the app from crashing unexpectedly

            // Catch unhandled exceptions from background threads
            // Example: Timer events, async operations without await
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                // Cast the exception object to Exception type
                var ex = args.ExceptionObject as Exception;

                // Show error message to user
                MessageBox.Show(
                    $"Unhandled Error:\n{ex?.Message}",
                    "Fatal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Shutdown the application with error code 1
                // (0 = success, 1 = error)
                Shutdown(1);
            };

            // Catch unhandled exceptions from UI thread (Dispatcher)
            // Example: Button click handlers, data binding errors
            DispatcherUnhandledException += (sender, args) =>
            {
                // Show warning to user (not fatal, app can continue)
                MessageBox.Show(
                    $"UI Error:\n{args.Exception.Message}",
                    "UI Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Mark exception as handled to prevent crash
                // The app will continue running
                args.Handled = true;
            };

            // ========== STEP 4: START THE APPLICATION ==========

            // Start the host (initializes all services)
            // ConfigureAwait(false) prevents deadlocks in async code
            await AppHost.StartAsync().ConfigureAwait(false);

            // Get MainWindow from DI container
            // This automatically injects MainViewModel and all dependencies
            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();

            // Show the main window
            mainWindow.Show();
        }
#pragma warning disable CA1031 // Generic catch acceptable at app entry point
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // If startup fails completely, show critical error
            MessageBox.Show(
                $"Critical startup error:\n{ex.Message}",
                "Fatal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Shutdown with error code
            Shutdown(1);
        }
    }

    /// <summary>
    /// Called when the application exits.
    /// This ensures all services are properly shut down and resources are released.
    /// </summary>
    /// <param name="e">Exit event arguments</param>
    protected override async void OnExit(ExitEventArgs e)
    {
        // Only dispose if AppHost was created successfully
        if (AppHost is not null)
        {
            // Stop the host gracefully
            // This calls Dispose on all singleton services
            await AppHost.StopAsync().ConfigureAwait(false);

            // Release all resources held by the host
            AppHost.Dispose();
        }

        // Call base class cleanup
        base.OnExit(e);
    }
}
