namespace ProjectFlow.Api.DTOs;

public class AnalyticsResponse
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int InactiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int PlanningProjects { get; set; }

    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int OverdueTasks { get; set; }

    public int TeamMembers { get; set; }
    public int ActiveMembers { get; set; }

    public double CompletionRate { get; set; }

    public List<MonthlyCompletedTasks> MonthlyCompletedTasks { get; set; } = new();

    public List<ProjectPerformance> ProjectPerformance { get; set; } = new();

    public List<TeamPerformance> TeamPerformance { get; set; } = new();
}

public class MonthlyCompletedTasks
{
    public string Month { get; set; } = string.Empty;
    public int Completed { get; set; }
}

public class ProjectPerformance
{
    public string Name { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
}

public class TeamPerformance
{
    public string Name { get; set; } = string.Empty;
    public int CompletedTasks { get; set; }
    public int TotalTasks { get; set; }
    public double CompletionRate { get; set; }
}