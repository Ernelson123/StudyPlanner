using StudyPlanner.Core.Services;

namespace StudyPlanner.Presentation.Services;

public class DesktopNotificationService : INotificationService
{
    public Task NotifyAsync(string message, CancellationToken ct = default)
    {
        // Placeholder: substituiremos por notificações reais na Fase 5
        Console.WriteLine($"[Notification] {message}");
        return Task.CompletedTask;
    }
}
