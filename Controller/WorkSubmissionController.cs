using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/work-submissions")]
[Authorize(Policy = Permissions.SubmitWork)]
public class WorkSubmissionsController : GenericApiControllerBase
{
    private readonly WorkSubmissionService _service;

    public WorkSubmissionsController(
        WorkSubmissionService service)
    {
        _service = service;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
    }

    /*
     * GET /api/work-submissions/my
     */
    [HttpGet("my")]
    public async Task<GenericApiResponse<object?>> GetMySubmissions()
    {
        var employeeId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(employeeId))
            return Failure(
                StatusCodes.Status401Unauthorized,
                "User identity not found.",
                "USER_IDENTITY_NOT_FOUND");

        var submissions =
            await _service.GetEmployeeSubmissionsAsync(
                employeeId);

        return Success(submissions, "Work submissions loaded.");
    }

    /*
     * GET /api/work-submissions/task/{taskId}
     */
    [HttpGet("task/{taskId}")]
    public async Task<GenericApiResponse<object?>> GetTaskSubmission(
        string taskId)
    {
        var employeeId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(employeeId))
            return Failure(
                StatusCodes.Status401Unauthorized,
                "User identity not found.",
                "USER_IDENTITY_NOT_FOUND");

        var submission =
            await _service.GetByTaskAsync(
                taskId,
                employeeId);

        return Success(submission, "Task submission loaded.");
    }

    /*
     * POST /api/work-submissions
     */
    [HttpPost]
    public async Task<GenericApiResponse<object?>> SubmitWork(
        [FromBody] CreateWorkSubmissionRequest request)
    {
        var employeeId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(employeeId))
            return Failure(
                StatusCodes.Status401Unauthorized,
                "User identity not found.",
                "USER_IDENTITY_NOT_FOUND");

        try
        {
            var submission =
                await _service.CreateAsync(
                    request,
                    employeeId);

            return Success(submission, "Work submitted successfully.");
        }
        catch (ArgumentException ex)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                ex.Message,
                "INVALID_WORK_SUBMISSION");
        }
    }
}