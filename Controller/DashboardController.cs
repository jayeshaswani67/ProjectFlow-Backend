using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Permissions.ViewDashboard)]
public class DashboardController : GenericApiControllerBase
{
    private readonly DashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        DashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<GenericApiResponse<object?>> GetDashboard()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(role))
        {
            return Failure(
                StatusCodes.Status403Forbidden,
                "User role not found.",
                "USER_ROLE_NOT_FOUND");
        }

        var isAdmin = string.Equals(
            role,
            "Admin",
            StringComparison.OrdinalIgnoreCase);
        var userId = isAdmin
            ? null
            : User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!isAdmin && string.IsNullOrWhiteSpace(userId))
        {
            return Failure(
                StatusCodes.Status401Unauthorized,
                "User identity not found.",
                "USER_IDENTITY_NOT_FOUND");
        }

        try
        {
            var dashboard = await _dashboardService.GetDashboardAsync(role, userId);
            return Success(dashboard, "Dashboard retrieved successfully.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load {Role} dashboard for user {UserId}.",
                role,
                userId);

            return Failure(
                StatusCodes.Status500InternalServerError,
                $"Failed to load {role.ToLowerInvariant()} dashboard.",
                "DASHBOARD_LOAD_FAILED");
        }
    }
}
