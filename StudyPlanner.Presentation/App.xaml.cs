using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Services;
using StudyPlanner.Data;
using StudyPlanner.Data.Repositories;
using StudyPlanner.Presentation.Services;
using StudyPlanner.Presentation.ViewModels;
using System.Globalization;
using System.Windows;

namespace StudyPlanner.Presentation
{


    public partial class App : System.Windows.Application
    {
        public static IHost? AppHost { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Use system culture safely
                var systemCulture = System.Globalization.CultureInfo.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = systemCulture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = systemCulture;
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = systemCulture;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = systemCulture;

                AppHost = Host.CreateDefaultBuilder()
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.SetMinimumLevel(LogLevel.Information);
                    })
                    .ConfigureServices((_, services) =>
                    {
                        services.AddSingleton<ITimerService, PomodoroTimerService>();
                        services.AddSingleton<ITimeAllocator, TimeAllocator>();
                        services.AddSingleton<INotificationService, DesktopNotificationService>();
                        services.AddScoped<IPlanningService, PlanningService>();
                        services.AddScoped<IStatisticsService, StatisticsService>();
                        services.AddDbContext<StudyPlannerDbContext>(options =>
                            options.UseSqlite("Data Source=studyplanner.db"));
                        services.AddTransient<ISubjectRepository, SubjectRepository>();
                        services.AddTransient<MainWindow>();
                        services.AddTransient<MainViewModel>();
                        services.AddTransient<IStudySessionService, StudyPlanner.Data.Services.StudySessionService>();
                        // Study Session Service (Data layer implementation)
                        services.AddTransient<IStudySessionService, StudyPlanner.Data.Services.StudySessionService>();

                    })
                    .Build();

                await AppHost.StartAsync().ConfigureAwait(false);

                var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                MessageBox.Show($"Critical startup error:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}",
                    "Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (AppHost is not null)
            {
                await AppHost.StopAsync().ConfigureAwait(false);
                AppHost.Dispose();
            }
            base.OnExit(e);
        }
    }
}
