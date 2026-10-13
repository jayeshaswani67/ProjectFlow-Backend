using ProjectFlow.Api.Models.Notifications;

namespace ProjectFlow.Api.Interfaces;

public interface INotificationService
{
    Task SendToUserAsync(
        string userId,
        NotificationMessage notification
    );

    Task SendToUsersAsync(
        IEnumerable<string> userIds,
        NotificationMessage notification
    );
}