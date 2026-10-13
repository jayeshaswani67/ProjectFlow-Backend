using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Models;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(Policy = Permissions.ViewProjects)]
public class ProjectsController : GenericApiControllerBase
{
    private readonly ProjectService _projectService;
    private readonly TaskService _taskService;

    public ProjectsController(
        ProjectService projectService,
        TaskService taskService)
    {
        _projectService = projectService;
        _taskService = taskService;
    }

    
    // CURRENT USER ID
    

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? string.Empty;


    
    // GET ALL PROJECTS
    // GET: /api/projects
    

    [HttpGet]
    public async Task<GenericApiResponse<object?>> GetProjects(
        int page = 1,
        int pageSize = 10)
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

        var result =
            await _projectService.GetProjectsAsync(
                UserId,
                page,
                pageSize);

        var totalPages =
            (int)Math.Ceiling(
                result.TotalCount / (double)pageSize);

        return Success(
            result.Projects,
            "Projects retrieved successfully.",
            meta: new
            {
                page,
                pageSize,
                totalItems = result.TotalCount,
                totalPages
            }
        );
    }
      [HttpGet("my")]
    public async Task<GenericApiResponse<object?>> GetMyProjects()
    {
        var employeeId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return Failure(
                StatusCodes.Status401Unauthorized,
                "User identity not found.",
                "USER_IDENTITY_NOT_FOUND");
        }

        var projects =
            await _projectService.GetMyProjectsAsync(employeeId);

        return Success(projects, "Projects retrieved successfully.");
    }

    // GET PROJECT BY ID
    // GET: /api/projects/{id}
    

    [HttpGet("{id}")]
    public async Task<GenericApiResponse<object?>> GetProject(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Project ID is required.",
                "PROJECT_ID_REQUIRED");
        }

        var project =
            await _projectService.GetProjectAsync(id);

        if (project == null)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                $"Project '{id}' was not found.",
                "PROJECT_NOT_FOUND");
        }

        return Success(project, "Project retrieved successfully.");
    }


    
    // CREATE TASK INSIDE PROJECT
    // POST: /api/projects/{projectId}/tasks
    

    [HttpPost("{projectId}/tasks")]
    [Authorize(Policy = Permissions.CreateTask)]
    public async Task<GenericApiResponse<object?>> CreateTask(
        string projectId,
        [FromBody] CreateTaskRequest request)
    {
        
        // Validate Project ID
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Project ID is required.",
                "PROJECT_ID_REQUIRED");
        }


        
        // Validate request
        if (request == null)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Task data is required.",
                "TASK_DATA_REQUIRED");
        }


        
        // Validate title
        

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Task title is required.",
                "TASK_TITLE_REQUIRED");
        }


        
        // Check whether project exists
        

        var project =
            await _projectService.GetProjectAsync(projectId);

        if (project == null)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                $"Project '{projectId}' was not found.",
                "PROJECT_NOT_FOUND");
        }


        
        // Create TaskItem from DTO
        

        var task = new TaskItem
        {
            ProjectId = projectId,

            Title = request.Title.Trim(),

            Description = request.Description ?? string.Empty,

            Status = string.IsNullOrWhiteSpace(request.Status)
                ? "Active"
                : request.Status,

            Priority = string.IsNullOrWhiteSpace(request.Priority)
                ? "Medium"
                : request.Priority,

            AssignedTo = request.AssignedTo ?? string.Empty,

            DueDate = request.DueDate,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow
        };


        
        // Create task
        

        await _taskService.CreateAsync(task);


        
        // Return created task
        

        return Success(
            task,
            "Task created successfully.",
            StatusCodes.Status201Created
        );
    }


    
    // CREATE PROJECT
    // POST: /api/projects
    

    [HttpPost]
    [Authorize(Policy = Permissions.CreateProject)]
    public async Task<GenericApiResponse<object?>> CreateProject(
        [FromBody] CreateProjectRequest request)
    {
        try
        {
            var project =
                await _projectService.CreateProjectAsync(
                    request,
                    UserId
                );

            Response.Headers.Location =
                Url.Action(nameof(GetProject), new { id = project.Id })
                ?? $"/api/projects/{project.Id}";

            return Success(
                project,
                "Project created successfully.",
                StatusCodes.Status201Created
            );
        }
        catch (ArgumentException ex)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                ex.Message,
                "INVALID_PROJECT");
        }
    }


    
    // UPDATE PROJECT
    // PUT: /api/projects/{id}
    

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.EditProject)]
    public async Task<GenericApiResponse<object?>> UpdateProject(
        string id,
        [FromBody] UpdateProjectRequest request)
    {
        ProjectResponse? project;
        try
        {
            project = await _projectService.UpdateProjectAsync(
                id,
                request,
                UserId);
        }
        catch (ArgumentException ex)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                ex.Message,
                "INVALID_PROJECT");
        }

        if (project == null)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "Project not found.",
                "PROJECT_NOT_FOUND");
        }

        return Success(project, "Project updated successfully.");
    }


    
    // DELETE PROJECT
    // DELETE: /api/projects/{id}
    

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.DeleteProject)]
    public async Task<GenericApiResponse<object?>> DeleteProject(string id)
    {
        var deleted =
            await _projectService.DeleteProjectAsync(
                id,
                UserId
            );

        if (!deleted)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "Project not found.",
                "PROJECT_NOT_FOUND");
        }

        return Success(null, "Project deleted successfully.");
    }
}
