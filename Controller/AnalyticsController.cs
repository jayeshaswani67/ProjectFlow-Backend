using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Policy = Permissions.ViewAnalytics)]
public class AnalyticsController : GenericApiControllerBase
{
    private readonly AnalyticsService _analyticsService;

    public AnalyticsController(
        AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<GenericApiResponse<object?>> GetAnalytics()
    {
        try
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            var role =
                User.FindFirstValue(
                    ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Failure(
                    StatusCodes.Status401Unauthorized,
                    "User identity not found.",
                    "USER_IDENTITY_NOT_FOUND"
                );
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                return Failure(
                    StatusCodes.Status401Unauthorized,
                    "User role not found.",
                    "USER_ROLE_NOT_FOUND"
                );
            }

            var analytics =
                await _analyticsService
                    .GetAnalyticsAsync(
                        userId,
                        role);

            return Success(
                analytics,
                "Analytics retrieved successfully."
            );
        }
        catch (UnauthorizedAccessException)
        {
            return Failure(
                StatusCodes.Status403Forbidden,
                "You are not authorized to view analytics.",
                "ANALYTICS_FORBIDDEN"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Analytics error: {ex}");

            return Failure(
                StatusCodes.Status500InternalServerError,
                "Failed to load analytics.",
                "ANALYTICS_LOAD_FAILED"
            );
        }
    }
}