
using MongoDB.Driver;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public class TaskService
{
    private readonly MongoDbContext _db;

    public TaskService(MongoDbContext db)
    {
        _db = db;
    }
    

    public async Task CreateAsync(TaskItem task)
    {
        task.Id = string.Empty;

        if (string.IsNullOrWhiteSpace(task.Status))
        {
            task.Status = "Pending";
        }

        if (string.IsNullOrWhiteSpace(task.Priority))
        {
            task.Priority = "Medium";
        }

        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.Tasks.InsertOneAsync(task);
    }
    public async Task<(List<TaskItem> Tasks, long TotalCount)> GetAllAsync(
        int page,
        int pageSize)
    {
        return await GetPagedAsync(
            Builders<TaskItem>.Filter.Empty,
            Builders<TaskItem>.Sort.Descending(t => t.CreatedAt)
                .Ascending(t => t.Id),
            page,
            pageSize);
    }
    public async Task<(List<TaskItem> Tasks, long TotalCount)> GetMyTasksAsync(
        string employeeId,
        int page,
        int pageSize)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return ([], 0);
        }

        return await GetPagedAsync(
            Builders<TaskItem>.Filter.Eq(x => x.AssignedTo, employeeId),
            Builders<TaskItem>.Sort.Descending(x => x.CreatedAt)
                .Ascending(x => x.Id),
            page,
            pageSize);
    }


private async Task<(List<TaskItem> Tasks, long TotalCount)> GetPagedAsync(
    FilterDefinition<TaskItem> filter,
    SortDefinition<TaskItem> sort,
    int page,
    int pageSize)
{
    var totalCount = await _db.Tasks.CountDocumentsAsync(filter);

    var skip = ((long)page - 1) * pageSize;
    if (skip > int.MaxValue)
    {
        return ([], totalCount);
    }

    var tasks = await _db.Tasks
        .Find(filter)
        .Sort(sort)
        .Skip((int)skip)
        .Limit(pageSize)
        .ToListAsync();

    return (tasks, totalCount);
}

//task summary

    public async Task<TaskSummary> GetSummaryAsync()
    {
        var tasks = await _db.Tasks
            .Find(Builders<TaskItem>.Filter.Empty)
            .ToListAsync();

        var now = DateTime.UtcNow;

        var today = now.Date;

        var tomorrow = today.AddDays(1);


        return new TaskSummary
        {
            // Total number of tasks
            Total = tasks.Count,


            // Completed tasks
            Completed = tasks.Count(
                t => t.Status == "Completed"
            ),


            // In Progress tasks
            InProgress = tasks.Count(
                t => t.Status == "In Progress"
            ),


            // Pending tasks
            Pending = tasks.Count(
                t => t.Status == "Pending"
            ),


            // Tasks due today
            Today = tasks.Count(
                t =>
                    t.DueDate >= today &&
                    t.DueDate < tomorrow &&
                    t.Status != "Completed"
            ),


            // Tasks whose due date has passed
            // and are not completed
            Overdue = tasks.Count(
                t =>
                    t.DueDate < now &&
                    t.Status != "Completed"
            )
        };
    }


    
    // GET TASK BY ID
    
    //
    // This is NOT a public GET endpoint anymore.
    //
    // It is used internally by:
    // PUT /api/tasks/{id}
    // PATCH /api/tasks/{id}/status
    //
    

    public async Task<TaskItem?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return await _db.Tasks
            .Find(t => t.Id == id)
            .FirstOrDefaultAsync();
    }


    
    // UPDATE TASK
    
    //
    // Used by:
    // PUT /api/tasks/{id}
    //
    

    public async Task<TaskItem?> UpdateAsync(
        string id,
        TaskItem updatedTask)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var existingTask = await _db.Tasks
            .Find(t => t.Id == id)
            .FirstOrDefaultAsync();

        if (existingTask == null)
        {
            return null;
        }


        // Keep the original ID
        updatedTask.Id = existingTask.Id;

        // Keep the original creation date
        updatedTask.CreatedAt = existingTask.CreatedAt;

        // Update modification date
        updatedTask.UpdatedAt = DateTime.UtcNow;


        // Preserve ProjectId if it wasn't supplied
        if (string.IsNullOrWhiteSpace(updatedTask.ProjectId))
        {
            updatedTask.ProjectId = existingTask.ProjectId;
        }


        await _db.Tasks.ReplaceOneAsync(
            t => t.Id == id,
            updatedTask
        );

        return updatedTask;
    }


    
    // UPDATE TASK STATUS
    
    //
    // Used by:
    // PATCH /api/tasks/{id}/status
    //
    

    public async Task<TaskItem?> UpdateStatusAsync(
        string id,
        string status)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var existingTask = await _db.Tasks
            .Find(t => t.Id == id)
            .FirstOrDefaultAsync();

        if (existingTask == null)
        {
            return null;
        }


        existingTask.Status = status;
        existingTask.UpdatedAt = DateTime.UtcNow;


        await _db.Tasks.ReplaceOneAsync(
            t => t.Id == id,
            existingTask
        );

        return existingTask;
    }


    
    // DELETE TASK
    
    //
    // Used by:
    // DELETE /api/tasks/{id}
    //
    

    public async Task<bool> DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var result = await _db.Tasks.DeleteOneAsync(
            t => t.Id == id
        );

        return result.DeletedCount > 0;
    }
}

public class TaskSummary
{
    public int InProgress { get; internal set; }
    public int Pending { get; internal set; }
    public int Total { get; internal set; }
    public int Completed { get; internal set; }
    public int Today { get; internal set; }
    public int Overdue { get; internal set; }
}