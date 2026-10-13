
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.Models;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize(Policy = Permissions.ViewTasks)]
public class TasksController : GenericApiControllerBase
{
    private readonly TaskService _taskService;

    public TasksController(TaskService taskService)
    {
        _taskService = taskService;
    }
    [HttpGet]
    public async Task<GenericApiResponse<object?>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
{
    var validation = ValidatePagination(page, pageSize);
    if (validation != null)
    {
        return validation;
    }

    var result = await _taskService.GetAllAsync(page, pageSize);

    return Success(
        result.Tasks,
        "tasks retrieved successfully",
        meta: BuildPaginationMeta(page, pageSize, result.TotalCount)
    );
}
    [HttpGet("my")]
    public async Task<GenericApiResponse<object?>> GetMyTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var validation = ValidatePagination(page, pageSize);
        if (validation != null)
        {
            return validation;
        }

        var employeeId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return Failure(
                StatusCodes.Status401Unauthorized,
                "User identity not found.",
                "USER_IDENTITY_NOT_FOUND");
        }

        var result =
            await _taskService.GetMyTasksAsync(employeeId, page, pageSize);

        return Success(
            result.Tasks,
            "Tasks retrieved successfully.",
            meta: BuildPaginationMeta(page, pageSize, result.TotalCount)
        );
    }

 

    [HttpGet("summary")]
    public async Task<GenericApiResponse<object?>> GetSummary()
    {
        var summary = await _taskService.GetSummaryAsync();

        return Success(summary, "Summary retrieved successfully.");
    }
    
    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.EditTask)]
    public async Task<GenericApiResponse<object?>> Update(
        string id,
        [FromBody] TaskItem updatedTask)
    {
        if (string.IsNullOrWhiteSpace(updatedTask.Title))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Task title is required.",
                "VALIDATION_ERROR");
        }

        var existingTask =
            await _taskService.GetByIdAsync(id);

        if (existingTask == null)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "Task not found.",
                "NOT_FOUND");
        }

        var role = User.FindFirst(
            System.Security.Claims.ClaimTypes.Role
        )?.Value;

        var userId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier
        )?.Value;

        // Developer can only update their own assigned task.
        if (role == "Developer" &&
            existingTask.AssignedTo != userId)
        {
            return Failure(
                StatusCodes.Status403Forbidden,
                "You are not authorized to update this task.",
                "TASK_UPDATE_FORBIDDEN");
        }

        var task =
            await _taskService.UpdateAsync(
                id,
                updatedTask
            );

        return Success(task, "Task updated successfully.");
    }


    //task-status

    [HttpPatch("{id}/status")]
    [Authorize(Policy = Permissions.EditTask)]
    public async Task<GenericApiResponse<object?>> UpdateStatus(
        string id,
        [FromBody] UpdateTaskStatusRequest request)
    {
        var allowedStatuses = new[]
        {
            "Pending",
            "In Progress",
            "Completed"
        };

        if (!allowedStatuses.Contains(request.Status))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Invalid task status.",
                "VALIDATION_ERROR");
        }

        var existingTask =
            await _taskService.GetByIdAsync(id);

        if (existingTask == null)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "Task not found.",
                "NOT_FOUND");
        }

        var role = User.FindFirst(
            System.Security.Claims.ClaimTypes.Role
        )?.Value;

        var userId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier
        )?.Value;

        // Developer can update only their assigned task.
        if (role == "Developer" &&
            existingTask.AssignedTo != userId)
        {
            return Failure(
                StatusCodes.Status403Forbidden,
                "You are not authorized to update this task.",
                "TASK_UPDATE_FORBIDDEN");
        }

        var task =
            await _taskService.UpdateStatusAsync(
                id,
                request.Status
            );

        return Success(task, "Task status updated successfully.");
    }


    
    // DELETE /api/tasks/{id}
    
    //
    // Only Admin and Manager can delete tasks.
    //
    

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.DeleteTask)]
    public async Task<GenericApiResponse<object?>> Delete(string id)
    {
        var deleted =
            await _taskService.DeleteAsync(id);

        if (!deleted)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "Task not found.",
                "NOT_FOUND");
        }

        return Success(null, "Task deleted successfully.");
    }

    private GenericApiResponse<object?>? ValidatePagination(
        int page,
        int pageSize)
    {
        if (page < 1)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Page must be greater than or equal to 1.",
                "INVALID_PAGE");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Page size must be between 1 and 100.",
                "INVALID_PAGE_SIZE");
        }

        return null;
    }

    private static object BuildPaginationMeta(
        int page,
        int pageSize,
        long totalItems)
    {
        return new
        {
            page,
            pageSize,
            totalItems,
            totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize)
        };
    }
}

internal class TaskResponse
{
}



// STATUS REQUEST


public class UpdateTaskStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
