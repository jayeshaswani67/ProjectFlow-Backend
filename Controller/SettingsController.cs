using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Policy = Permissions.SystemSettings)]
public class SettingsController : GenericApiControllerBase
{
    private readonly SettingsService _settingsService;

    public SettingsController(
        SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    // GET /api/settings
    [HttpGet]
    public async Task<GenericApiResponse<object?>> GetSettings()
    {
        var settings =
            await _settingsService.GetAsync();

        return Success(settings, "Settings retrieved successfully.");
    }

    // PUT /api/settings
    [HttpPut]
    public async Task<GenericApiResponse<object?>> UpdateSettings(
        [FromBody] UpdateSettingsRequest request)
    {
        var settings =
            await _settingsService.UpdateAsync(
                request
            );

        return Success(settings, "Settings updated successfully.");
    }
}