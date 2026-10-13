using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/ai-planner")]
[Authorize(Policy = Permissions.UseAIPlanner)]
public class AIPlannerController : GenericApiControllerBase
{
private readonly AIPlannerService _plannerService;

public AIPlannerController(
    AIPlannerService plannerService)
{
    _plannerService = plannerService;
}

[HttpPost("generate")]
public async Task<GenericApiResponse<object?>> Generate(
    [FromBody] GeneratePlannerRequest request)
{
    if (string.IsNullOrWhiteSpace(request.ProjectName))
    {
        return Failure(StatusCodes.Status400BadRequest, "Project name is required.", "PROJECT_NAME_REQUIRED");
    }

    if (string.IsNullOrWhiteSpace(request.Description))
    {
        return Failure(StatusCodes.Status400BadRequest, "Project description is required.", "PROJECT_DESCRIPTION_REQUIRED");
    }

    if (request.TeamSize < 1)
    {
        return Failure(StatusCodes.Status400BadRequest, "Team size must be at least 1.", "INVALID_TEAM_SIZE");
    }

    if (request.Deadline.Date <= DateTime.UtcNow.Date)
    {
        return Failure(StatusCodes.Status400BadRequest, "Deadline must be in the future.", "INVALID_DEADLINE");
    }

    try
    {
        var plan =
            await _plannerService.GeneratePlanAsync(
                request
            );

        return Success(plan, "Project plan generated successfully.");
    }
    catch (ArgumentException ex)
    {
        return Failure(StatusCodes.Status400BadRequest, ex.Message, "INVALID_REQUEST");
    }
    catch (InvalidOperationException ex)
    {
        return Failure(
            StatusCodes.Status502BadGateway,
            ex.Message,
            "PLANNER_SERVICE_UNAVAILABLE"
        );
    }
    catch (Exception)
    {
        return Failure(
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred while generating the project plan.",
            "INTERNAL_SERVER_ERROR"
        );
    }
}

}
