using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.Interfaces;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : GenericApiControllerBase
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<GenericApiResponse<object?>> GetMyProfile()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrWhiteSpace(userId))
            return Failure(
                StatusCodes.Status401Unauthorized,
                "User identity not found.",
                "USER_IDENTITY_NOT_FOUND");

        var profile = await _userService.GetProfileAsync(userId);

        if (profile is null)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "User profile not found.",
                "USER_PROFILE_NOT_FOUND");
        }

        return Success(profile, "Profile loaded successfully.");
    }

    [HttpPut("me")]
    public async Task<GenericApiResponse<object?>> UpdateMyProfile(
        UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrWhiteSpace(userId))
            return Failure(
                StatusCodes.Status401Unauthorized,
                "User identity not found.",
                "USER_IDENTITY_NOT_FOUND");

        var profile = await _userService.UpdateProfileAsync(
            userId,
            request.Name
        );

        if (profile is null)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "User profile not found.",
                "USER_PROFILE_NOT_FOUND");
        }

        return Success(profile, "Profile updated successfully.");
    }
}

public sealed class UpdateProfileRequest
{
    public string Name { get; set; } = "";
}