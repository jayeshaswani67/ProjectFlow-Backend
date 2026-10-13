namespace ProjectFlow.Api.Models.Notifications;

public class NotificationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Type { get; set; } = "";

    public string Title { get; set; } = "";

    public string Message { get; set; } = "";

    public string? EntityId { get; set; }

    public string? EntityType { get; set; }

    public string? ActionUrl { get; set; }

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;
}