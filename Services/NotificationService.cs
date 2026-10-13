using Microsoft.AspNetCore.SignalR;
using ProjectFlow.Api.Hubs;
using ProjectFlow.Api.Interfaces;
using ProjectFlow.Api.Models.Notifications;

namespace ProjectFlow.Api.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(
        IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(
        string userId,
        NotificationMessage notification)
    {
        await _hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync(
                "ReceiveNotification",
                notification
            );
    }

    public async Task SendToUsersAsync(
        IEnumerable<string> userIds,
        NotificationMessage notification)
    {
        var tasks = userIds.Select(
            userId => SendToUserAsync(
                userId,
                notification
            )
        );

        await Task.WhenAll(tasks);
    }
}