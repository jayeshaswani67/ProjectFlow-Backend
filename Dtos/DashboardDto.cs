namespace ProjectFlow.Api.DTOs;

public class DashboardStatsDto
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int PlanningProjects { get; set; }
    public int OnHoldProjects { get; set; }

    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }

    public int TeamMembers { get; set; }
    public int ActiveMembers { get; set; }

    public int PendingReviews { get; set; }

    public double CompletionRate { get; set; }
}


public class DashboardProjectDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public DateTime? DueDate { get; set; }

    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }

    public double Progress { get; set; }
}


public class DashboardChartPointDto
{
    public string Label { get; set; } = "";
    public int Value { get; set; }
}


public class DashboardProjectChartDto
{
    public string Name { get; set; } = "";
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double Progress { get; set; }
}


public class DashboardTeamChartDto
{
    public string Name { get; set; } = "";
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CompletionRate { get; set; }
}


public class DashboardResponseDto
{
    public string Role { get; set; } = "";

    public DashboardStatsDto Stats { get; set; } = new();

    public List<DashboardProjectDto> RecentProjects { get; set; } = new();

    public List<DashboardChartPointDto> TaskStatusChart { get; set; } = new();

    public List<DashboardChartPointDto> MonthlyTasksChart { get; set; } = new();

    public List<DashboardProjectChartDto> ProjectPerformance { get; set; } = new();

    public List<DashboardTeamChartDto> TeamPerformance { get; set; } = new();
}