using MongoDB.Bson;
using MongoDB.Driver;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public class DashboardService
{
    private readonly MongoDbContext _db;

    public DashboardService(MongoDbContext db)
    {
        _db = db;
    }

    public Task<DashboardResponseDto> GetDashboardAsync(string role, string? userId)
    {
        return role switch
        {
            "Admin" => GetAdminDashboardAsync(),
            "Manager" when !string.IsNullOrWhiteSpace(userId) =>
                GetManagerDashboardAsync(userId),
            "Employee" when !string.IsNullOrWhiteSpace(userId) =>
                GetEmployeeDashboardAsync(userId),
            _ => throw new ArgumentException(
                "A supported dashboard role and its user ID are required.")
        };
    }

    private async Task<DashboardResponseDto> GetAdminDashboardAsync()
    {
        var projectsTask = _db.Projects.Find(_ => true).ToListAsync();
        var tasksTask = _db.Tasks.Find(_ => true).ToListAsync();
        var usersTask = _db.Users.Find(_ => true).ToListAsync();

        await Task.WhenAll(projectsTask, tasksTask, usersTask);

        var projects = await projectsTask;
        var tasks = await tasksTask;
        var users = await usersTask;

        return BuildDashboard("Admin", projects, tasks, users);
    }

    private async Task<DashboardResponseDto> GetManagerDashboardAsync(string managerId)
    {
        var manager = await _db.Users
            .Find(x => x.Id == managerId)
            .FirstOrDefaultAsync();

        var projectFilters = new List<FilterDefinition<Project>>
        {
            Builders<Project>.Filter.Eq(x => x.OwnerId, managerId),
            Builders<Project>.Filter.Eq(x => x.CreatedBy, managerId),
            Builders<Project>.Filter.Eq(x => x.ManagerId, managerId)
        };

        if (!string.IsNullOrWhiteSpace(manager?.TeamId))
        {
            projectFilters.Add(
                Builders<Project>.Filter.Eq(x => x.TeamId, manager.TeamId));
        }

        var projects = await _db.Projects
            .Find(Builders<Project>.Filter.Or(projectFilters))
            .ToListAsync();

        var projectIds = projects
            .Select(x => x.Id)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var tasks = projectIds.Count == 0
            ? new List<TaskItem>()
            : await _db.Tasks
                .Find(Builders<TaskItem>.Filter.In(x => x.ProjectId, projectIds))
                .ToListAsync();

        var relevantUserIds = projects
            .SelectMany(x =>
                (x.MemberIds ?? new List<string>())
                    .Concat(x.TeamMembers ?? new List<string>()))
            .Concat(tasks.Select(x => x.AssignedTo))
            .Append(managerId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Where(x => ObjectId.TryParse(x, out _))
            .ToList();

        var userFilters = new List<FilterDefinition<User>>
        {
            Builders<User>.Filter.In(x => x.Id, relevantUserIds)
        };

        if (!string.IsNullOrWhiteSpace(manager?.TeamId))
        {
            userFilters.Add(
                Builders<User>.Filter.Eq(x => x.TeamId, manager.TeamId));
        }

        var users = await _db.Users
            .Find(Builders<User>.Filter.Or(userFilters))
            .ToListAsync();

        return BuildDashboard("Manager", projects, tasks, users);
    }

    private async Task<DashboardResponseDto> GetEmployeeDashboardAsync(string employeeId)
    {
        var tasks = await _db.Tasks
            .Find(x => x.AssignedTo == employeeId)
            .ToListAsync();

        var projectIds = tasks
            .Select(x => x.ProjectId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var projects = projectIds.Count == 0
            ? new List<Project>()
            : await _db.Projects
                .Find(Builders<Project>.Filter.In(x => x.Id, projectIds))
                .ToListAsync();

        var employee = await _db.Users
            .Find(x => x.Id == employeeId)
            .FirstOrDefaultAsync();
        var users = employee is null ? new List<User>() : new List<User> { employee };

        return BuildDashboard("Employee", projects, tasks, users);
    }

    private static DashboardResponseDto BuildDashboard(
        string role,
        List<Project> projects,
        List<TaskItem> tasks,
        List<User> users)
    {
        return new DashboardResponseDto
        {
            Role = role,
            Stats = BuildStats(projects, tasks, users),
            RecentProjects = BuildRecentProjects(projects, tasks),
            TaskStatusChart = BuildTaskStatusChart(tasks),
            MonthlyTasksChart = BuildMonthlyTasksChart(tasks),
            ProjectPerformance = BuildProjectPerformance(projects, tasks),
            TeamPerformance = role == "Employee"
                ? new List<DashboardTeamChartDto>()
                : BuildTeamPerformance(users, tasks)
        };
    }

    private static DashboardStatsDto BuildStats(
        List<Project> projects,
        List<TaskItem> tasks,
        List<User> users)
    {
        var completedTasks = tasks.Count(IsCompleted);
        var totalTasks = tasks.Count;

        return new DashboardStatsDto
        {
            TotalProjects = projects.Count,
            ActiveProjects = projects.Count(x =>
                IsStatus(x.Status, "Active") || IsStatus(x.Status, "In Progress")),
            CompletedProjects = projects.Count(x => IsStatus(x.Status, "Completed")),
            PlanningProjects = projects.Count(x => IsStatus(x.Status, "Planning")),
            OnHoldProjects = projects.Count(x => IsStatus(x.Status, "On Hold")),
            TotalTasks = totalTasks,
            PendingTasks = tasks.Count(IsPending),
            InProgressTasks = tasks.Count(x => IsStatus(x.Status, "In Progress")),
            CompletedTasks = completedTasks,
            OverdueTasks = tasks.Count(IsOverdue),
            TeamMembers = users.Count,
            ActiveMembers = users.Count(x => IsStatus(x.Status, "Active")),
            CompletionRate = totalTasks == 0
                ? 0
                : Math.Round((double)completedTasks / totalTasks * 100, 1)
        };
    }

    private static List<DashboardChartPointDto> BuildTaskStatusChart(List<TaskItem> tasks)
    {
        return new List<DashboardChartPointDto>
        {
            new() { Label = "Pending", Value = tasks.Count(IsPending) },
            new() { Label = "In Progress", Value = tasks.Count(x => IsStatus(x.Status, "In Progress")) },
            new() { Label = "Completed", Value = tasks.Count(IsCompleted) },
            new() { Label = "Overdue", Value = tasks.Count(IsOverdue) }
        };
    }

    private static List<DashboardChartPointDto> BuildMonthlyTasksChart(List<TaskItem> tasks)
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return Enumerable.Range(0, 6)
            .Select(offset => currentMonth.AddMonths(offset - 5))
            .Select(month => new DashboardChartPointDto
            {
                Label = month.ToString("MMM"),
                Value = tasks.Count(x =>
                    x.CreatedAt.Year == month.Year &&
                    x.CreatedAt.Month == month.Month)
            })
            .ToList();
    }

    private static List<DashboardProjectDto> BuildRecentProjects(
        List<Project> projects,
        List<TaskItem> tasks)
    {
        return projects
            .OrderByDescending(x => x.CreatedAt)
            .Take(6)
            .Select(project =>
            {
                var projectTasks = tasks
                    .Where(x => x.ProjectId == project.Id)
                    .ToList();
                var completed = projectTasks.Count(IsCompleted);

                return new DashboardProjectDto
                {
                    Id = project.Id,
                    Name = project.Name,
                    Status = project.Status,
                    Priority = project.Priority,
                    DueDate = project.DueDate,
                    TotalTasks = projectTasks.Count,
                    CompletedTasks = completed,
                    Progress = CalculateProgress(projectTasks, project.Progress)
                };
            })
            .ToList();
    }

    private static List<DashboardProjectChartDto> BuildProjectPerformance(
        List<Project> projects,
        List<TaskItem> tasks)
    {
        return projects
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(project =>
            {
                var projectTasks = tasks
                    .Where(x => x.ProjectId == project.Id)
                    .ToList();
                var completed = projectTasks.Count(IsCompleted);

                return new DashboardProjectChartDto
                {
                    Name = project.Name,
                    TotalTasks = projectTasks.Count,
                    CompletedTasks = completed,
                    Progress = CalculateProgress(projectTasks, project.Progress)
                };
            })
            .ToList();
    }

    private static List<DashboardTeamChartDto> BuildTeamPerformance(
        List<User> users,
        List<TaskItem> tasks)
    {
        var namesById = users.ToDictionary(x => x.Id, x => x.Name);

        return tasks
            .Where(x => !string.IsNullOrWhiteSpace(x.AssignedTo))
            .GroupBy(x => x.AssignedTo)
            .Select(group =>
            {
                var completed = group.Count(IsCompleted);
                var name = namesById.TryGetValue(group.Key, out var userName)
                    ? userName
                    : group.Key;

                return new DashboardTeamChartDto
                {
                    Name = name,
                    TotalTasks = group.Count(),
                    CompletedTasks = completed,
                    CompletionRate = Math.Round((double)completed / group.Count() * 100, 1)
                };
            })
            .OrderByDescending(x => x.TotalTasks)
            .ToList();
    }

    private static double CalculateProgress(List<TaskItem> tasks, int fallbackProgress)
    {
        if (tasks.Count == 0)
        {
            return fallbackProgress;
        }

        var completed = tasks.Count(IsCompleted);
        return Math.Round((double)completed / tasks.Count * 100, 1);
    }

    private static bool IsCompleted(TaskItem task) => IsStatus(task.Status, "Completed");

    private static bool IsPending(TaskItem task) =>
        IsStatus(task.Status, "Pending") ||
        IsStatus(task.Status, "Todo") ||
        IsStatus(task.Status, "To Do");

    private static bool IsOverdue(TaskItem task) =>
        !IsCompleted(task) &&
        task.DueDate.HasValue &&
        task.DueDate.Value.Date < DateTime.UtcNow.Date;

    private static bool IsStatus(string? actual, string expected) =>
        string.Equals(actual?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
