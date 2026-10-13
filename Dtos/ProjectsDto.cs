namespace ProjectFlow.Api.DTOs;

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public string Status { get; set; } = "Active";

    public string Priority { get; set; } = "Medium";

    public DateTime DueDate { get; set; }

    public List<string> TeamMembers { get; set; } = new();
}

public class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = "Medium";

    public DateTime DueDate { get; set; }

    public List<string> TeamMembers { get; set; } = new();
}

public class ProjectResponse
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public int Progress { get; set; }

    public int TotalTasks { get; set; }

    public int CompletedTasks { get; set; }

    public List<string> TeamMembers { get; set; } = new();

    public DateTime DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}