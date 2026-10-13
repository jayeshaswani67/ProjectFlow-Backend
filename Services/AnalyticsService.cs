using MongoDB.Driver;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public class AnalyticsService
{
    private readonly MongoDbContext _db;

    public AnalyticsService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<AnalyticsResponse> GetAnalyticsAsync(
        string userId,
        string role)
    {
        var normalizedRole = role.Trim();

        var projects = normalizedRole switch
        {
            "Admin" => await _db.Projects.Find(_ => true).ToListAsync(),
            "Manager" => await GetManagerProjectsAsync(userId),
            "Developer" or "Employee" => await GetDeveloperProjectsAsync(userId),
            _ => throw new UnauthorizedAccessException("Invalid user role.")
        };

        var projectIds = projects.Select(x => x.Id).ToList();
        var tasks = await GetTasksAsync(normalizedRole, userId, projectIds);
        var members = await GetMembersAsync(normalizedRole, userId);

        return BuildResponse(projects, tasks, members);
    }

    private async Task<List<Project>> GetManagerProjectsAsync(string userId)
    {
        var user = await _db.Users
            .Find(x => x.Id == userId)
            .FirstOrDefaultAsync();

        var filters = new List<FilterDefinition<Project>>
        {
            Builders<Project>.Filter.Eq(x => x.OwnerId, userId),
            Builders<Project>.Filter.Eq(x => x.CreatedBy, userId),
            Builders<Project>.Filter.Eq(x => x.ManagerId, userId)
        };

        if (!string.IsNullOrWhiteSpace(user?.TeamId))
        {
            filters.Add(
                Builders<Project>.Filter.Eq(x => x.TeamId, user.TeamId));
        }

        return await _db.Projects
            .Find(Builders<Project>.Filter.Or(filters))
            .ToListAsync();
    }

    private async Task<List<Project>> GetDeveloperProjectsAsync(string userId)
    {
        var assignedTasks = await _db.Tasks
            .Find(x => x.AssignedTo == userId)
            .ToListAsync();

        var projectIds = assignedTasks
            .Select(x => x.ProjectId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var filters = new List<FilterDefinition<Project>>
        {
            Builders<Project>.Filter.AnyEq(x => x.MemberIds, userId),
            Builders<Project>.Filter.AnyEq(x => x.TeamMembers, userId)
        };

        if (projectIds.Count > 0)
        {
            filters.Add(Builders<Project>.Filter.In(x => x.Id, projectIds));
        }

        return await _db.Projects
            .Find(Builders<Project>.Filter.Or(filters))
            .ToListAsync();
    }

    private async Task<List<TaskItem>> GetTasksAsync(
        string role,
        string userId,
        List<string> projectIds)
    {
        if (role is "Admin" or "Manager")
        {
            return projectIds.Count == 0
                ? new List<TaskItem>()
                : await _db.Tasks
                    .Find(Builders<TaskItem>.Filter.In(x => x.ProjectId, projectIds))
                    .ToListAsync();
        }

        return await _db.Tasks
            .Find(x => x.AssignedTo == userId)
            .ToListAsync();
    }

    private async Task<List<TeamMember>> GetMembersAsync(
        string role,
        string userId)
    {
        var filter = role switch
        {
            "Admin" => Builders<TeamMember>.Filter.Empty,
            "Manager" => Builders<TeamMember>.Filter.Eq(x => x.OwnerId, userId),
            _ => Builders<TeamMember>.Filter.Eq(x => x.Id, userId)
        };

        return await _db.TeamMembers.Find(filter).ToListAsync();
    }

    private static AnalyticsResponse BuildResponse(
        List<Project> projects,
        List<TaskItem> tasks,
        List<TeamMember> members)
    {
        var completedTasks = tasks.Count(IsCompleted);
        var completedProjects = projects.Count(x => IsStatus(x.Status, "Completed"));
        var activeProjects = projects.Count(x =>
            IsStatus(x.Status, "Active") || IsStatus(x.Status, "In Progress"));
        var inactiveProjects = projects.Count(x => IsStatus(x.Status, "Inactive"));

        return new AnalyticsResponse
        {
            TotalProjects = projects.Count,
            ActiveProjects = activeProjects,
            InactiveProjects = inactiveProjects,
            CompletedProjects = completedProjects,
            PlanningProjects = projects.Count(x => IsStatus(x.Status, "Planning")),
            TotalTasks = tasks.Count,
            CompletedTasks = completedTasks,
            PendingTasks = tasks.Count - completedTasks,
            OverdueTasks = tasks.Count(IsOverdue),
            TeamMembers = members.Count,
            ActiveMembers = members.Count(x => IsStatus(x.Status, "Active")),
            CompletionRate = tasks.Count == 0
                ? 0
                : Math.Round((double)completedTasks / tasks.Count * 100, 2),
            MonthlyCompletedTasks = BuildMonthlyCompletedTasks(tasks),
            ProjectPerformance = projects.Select(project =>
            {
                var projectTasks = tasks.Where(x => x.ProjectId == project.Id).ToList();
                var projectCompleted = projectTasks.Count(IsCompleted);

                return new ProjectPerformance
                {
                    Name = project.Name,
                    Progress = projectTasks.Count == 0
                        ? project.Progress
                        : (int)Math.Round((double)projectCompleted / projectTasks.Count * 100),
                    TotalTasks = projectTasks.Count,
                    CompletedTasks = projectCompleted
                };
            }).ToList(),
            TeamPerformance = BuildTeamPerformance(tasks, members)
        };
    }

    private static List<MonthlyCompletedTasks> BuildMonthlyCompletedTasks(
        List<TaskItem> tasks)
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return Enumerable.Range(0, 6)
            .Select(offset => currentMonth.AddMonths(offset - 5))
            .Select(month => new MonthlyCompletedTasks
            {
                Month = month.ToString("MMM"),
                Completed = tasks.Count(x =>
                    IsCompleted(x) &&
                    x.UpdatedAt.Year == month.Year &&
                    x.UpdatedAt.Month == month.Month)
            })
            .ToList();
    }

    private static List<TeamPerformance> BuildTeamPerformance(
        List<TaskItem> tasks,
        List<TeamMember> members)
    {
        var memberNames = members.ToDictionary(x => x.Id, x => x.Name);

        return tasks
            .Where(x => !string.IsNullOrWhiteSpace(x.AssignedTo))
            .GroupBy(x => x.AssignedTo)
            .Select(group =>
            {
                var completed = group.Count(IsCompleted);

                return new TeamPerformance
                {
                    Name = memberNames.GetValueOrDefault(group.Key, group.Key),
                    TotalTasks = group.Count(),
                    CompletedTasks = completed,
                    CompletionRate = Math.Round((double)completed / group.Count() * 100, 2)
                };
            })
            .OrderByDescending(x => x.CompletedTasks)
            .ToList();
    }

    private static bool IsCompleted(TaskItem task) =>
        IsStatus(task.Status, "Completed");

    private static bool IsOverdue(TaskItem task) =>
        !IsCompleted(task) && task.DueDate.HasValue && task.DueDate.Value.Date < DateTime.UtcNow.Date;

    private static bool IsStatus(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
}
