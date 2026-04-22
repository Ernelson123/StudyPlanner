namespace StudyPlanner.Core.Services;

public interface INotificationService
{
    Task NotifyAsync(string message, CancellationToken ct = default);
}
