
using MongoDB.Driver;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public class ProjectService
{
    private static readonly string[] AllowedProjectStatuses = ["Active", "Inactive"];

    private readonly MongoDbContext _db;

    public ProjectService(MongoDbContext db)
    {
        _db = db;
    }


    
    // GET PROJECTS
    

    public async Task<(
        List<ProjectResponse> Projects,
        long TotalCount)> GetProjectsAsync(
        string? userId = null,
        int page = 1,
        int pageSize = 10)
    {
        FilterDefinition<Project> filter;

        // If no userId is supplied, return all projects.
        if (string.IsNullOrWhiteSpace(userId))
        {
            filter = Builders<Project>.Filter.Empty;
        }
        else
        {
            // Current application behavior:
            // users see projects they own.
            filter = Builders<Project>.Filter.Eq(
                x => x.OwnerId,
                userId
            );
        }

        var totalCount = await _db.Projects
            .CountDocumentsAsync(filter);

        var skip = ((long)page - 1) * pageSize;
        if (skip > int.MaxValue)
        {
            return (new List<ProjectResponse>(), totalCount);
        }

        var projects = await _db.Projects
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((int)skip)
            .Limit(pageSize)
            .ToListAsync();

        return (
            projects
                .Select(MapToResponse)
                .ToList(),
            totalCount
        );
    }
    public async Task<List<ProjectResponse>> GetMyProjectsAsync(
        string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new List<ProjectResponse>();
        }

        var filter = Builders<Project>.Filter.Or(
            Builders<Project>.Filter.Eq(x => x.OwnerId, userId),
            Builders<Project>.Filter.Eq(x => x.CreatedBy, userId),
            Builders<Project>.Filter.Eq(x => x.ManagerId, userId),
            Builders<Project>.Filter.AnyEq(x => x.MemberIds, userId),
            Builders<Project>.Filter.AnyEq(x => x.TeamMembers, userId)
        );

        var projects = await _db.Projects
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return projects.Select(MapToResponse).ToList();
    }



    
    // GET PROJECT BY ID
    

    public async Task<ProjectResponse?> GetProjectAsync(
        string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var project = await _db.Projects
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (project == null)
        {
            return null;
        }

        return MapToResponse(project);
    }


    
    // GET PROJECT MODEL BY ID
    // Useful internally for services
    

    public async Task<Project?> GetProjectModelAsync(
        string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return await _db.Projects
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }


    
    // CREATE PROJECT
    

    public async Task<ProjectResponse> CreateProjectAsync(
        CreateProjectRequest request,
        string userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Project name is required."
            );
        }

        var project = new Project
        {
            Name = request.Name.Trim(),

            Description =
                request.Description?.Trim() ?? string.Empty,

            Category =
                string.IsNullOrWhiteSpace(request.Category)
                    ? "General"
                    : request.Category,

            Status = NormalizeProjectStatus(request.Status),

            Priority =
                string.IsNullOrWhiteSpace(request.Priority)
                    ? "Medium"
                    : request.Priority,

            DueDate = request.DueDate,

            TeamMembers =
                request.TeamMembers ?? new List<string>(),

            OwnerId = userId,

            Progress = 0,

            TotalTasks = 0,

            CompletedTasks = 0,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow
        };

        await _db.Projects.InsertOneAsync(project);

        return MapToResponse(project);
    }


    
    // UPDATE PROJECT
    

    public async Task<ProjectResponse?> UpdateProjectAsync(
        string id,
        UpdateProjectRequest request,
        string userId)
    {
        var project = await _db.Projects
            .Find(x =>
                x.Id == id &&
                x.OwnerId == userId
            )
            .FirstOrDefaultAsync();

        if (project == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            project.Name =
                request.Name.Trim();
        }

        project.Description =
            request.Description?.Trim()
            ?? string.Empty;

        project.Category =
            request.Category;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            project.Status = NormalizeProjectStatus(request.Status);
        }

        project.Priority =
            request.Priority;

        project.DueDate =
            request.DueDate;

        project.TeamMembers =
            request.TeamMembers
            ?? new List<string>();

        project.UpdatedAt =
            DateTime.UtcNow;

        await _db.Projects.ReplaceOneAsync(
            x => x.Id == id,
            project
        );

        return MapToResponse(project);
    }


    
    // DELETE PROJECT
    

    public async Task<bool> DeleteProjectAsync(
        string id,
        string userId)
    {
        var result =
            await _db.Projects.DeleteOneAsync(
                x =>
                    x.Id == id &&
                    x.OwnerId == userId
            );

        if (result.DeletedCount == 0)
        {
            return false;
        }

        // Delete all tasks belonging to the project.
        await _db.Tasks.DeleteManyAsync(
            x => x.ProjectId == id
        );

        return true;
    }


    
    // RECALCULATE PROJECT TASK STATISTICS
    //
    // This keeps:
    // TotalTasks
    // CompletedTasks
    // Progress
    //
    // synchronized with the Tasks collection.
    

    public async Task<bool> RecalculateTaskStatisticsAsync(
        string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return false;
        }

        // Check project exists.
        var project = await _db.Projects
            .Find(x => x.Id == projectId)
            .FirstOrDefaultAsync();

        if (project == null)
        {
            return false;
        }


        // Get all tasks belonging to project.
        var tasks = await _db.Tasks
            .Find(x => x.ProjectId == projectId)
            .ToListAsync();


        // Total tasks.
        var totalTasks = tasks.Count;


        // Completed tasks.
        var completedTasks = tasks.Count(
            x =>
                string.Equals(
                    x.Status,
                    "Completed",
                    StringComparison.OrdinalIgnoreCase
                )
        );


        // Calculate progress.
        var progress = totalTasks == 0
            ? 0
            : (int)Math.Round(
                completedTasks * 100.0 / totalTasks
            );


        // Update only task-related project fields.
        var update = Builders<Project>.Update
            .Set(x => x.TotalTasks, totalTasks)
            .Set(x => x.CompletedTasks, completedTasks)
            .Set(x => x.Progress, progress)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);


        var result = await _db.Projects.UpdateOneAsync(
            x => x.Id == projectId,
            update
        );

        return result.ModifiedCount > 0 ||
               result.MatchedCount > 0;
    }


    
    // MAP PROJECT → RESPONSE
    

    private static ProjectResponse MapToResponse(
        Project project)
    {
        return new ProjectResponse
        {
            Id = project.Id,

            Name = project.Name,

            Description =
                project.Description,

            Category =
                project.Category,

            Status =
                project.Status,

            Priority =
                project.Priority,

            Progress =
                project.Progress,

            TotalTasks =
                project.TotalTasks,

            CompletedTasks =
                project.CompletedTasks,

            TeamMembers =
                project.TeamMembers,

            DueDate =
                project.DueDate,

            CreatedAt =
                project.CreatedAt,

            UpdatedAt =
                project.UpdatedAt
        };
    }

    private static string NormalizeProjectStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "Active";
        }

        var normalizedStatus = AllowedProjectStatuses.FirstOrDefault(
            allowedStatus => string.Equals(
                status.Trim(),
                allowedStatus,
                StringComparison.OrdinalIgnoreCase));

        return normalizedStatus
            ?? throw new ArgumentException(
                "Project status must be either Active or Inactive.");
    }
}

