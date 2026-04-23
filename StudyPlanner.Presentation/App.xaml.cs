using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StudyPlanner.Application.Services;
using StudyPlanner.Core.Services;
using StudyPlanner.Data;
using StudyPlanner.Presentation.Services;
using System.Windows;

namespace StudyPlanner.Presentation;

public partial class App : System.Windows.Application
{
    public static IHost? AppHost { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<PomodoroTimerService>();
                    services.AddSingleton<INotificationService, DesktopNotificationService>();
                    services.AddScoped<IPlanningService, PlanningService>();
                    services.AddScoped<IStatisticsService, StatisticsService>();
                    services.AddSingleton<ITimerService, PomodoroTimerService>();
                    services.AddSingleton<ITimeAllocator, TimeAllocator>();
                    services.AddDbContext<StudyPlannerDbContext>(options =>
                        options.UseSqlite("Data Source=studyplanner.db"));
                    services.AddTransient<MainWindow>();
                })
                .Build();

            await AppHost.StartAsync().ConfigureAwait(false);
            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
#pragma warning disable CA1031 // Startup crash precisa ser genérico
        catch (Exception ex)
#pragma warning restore CA1031
        {
            MessageBox.Show($"Critical startup error:\n{ex.Message}", "Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
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
