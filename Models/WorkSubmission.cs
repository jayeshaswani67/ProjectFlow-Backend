namespace ProjectFlow.Api.Models;

public class WorkSubmission
{
    public string Id { get; set; } = "";

    public string TaskId { get; set; } = "";

    public string EmployeeId { get; set; } = "";

    public string Notes { get; set; } = "";

    public string? AttachmentUrl { get; set; }

    public string Status { get; set; } = "PendingReview";

    public string? ManagerFeedback { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }
}