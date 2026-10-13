using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.ManageUsers)]
public class UsersController : GenericApiControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<GenericApiResponse<object?>> GetUsers()
    {
        var users = await _userService.GetAllAsync();

        return Success(users, "Users retrieved successfully.");
    }

    [HttpPost]
    public async Task<GenericApiResponse<object?>> CreateUser(
        [FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "The request is invalid.",
                "VALIDATION_ERROR");
        }

        var user = await _userService.CreateAsync(request);

        return Success(
            user,
            "User created successfully.",
            StatusCodes.Status201Created
        );
    }

    [HttpPut("{id}")]
    public async Task<GenericApiResponse<object?>> UpdateUser(
        string id,
        [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "The request is invalid.",
                "VALIDATION_ERROR");
        }

        var user =
            await _userService.UpdateAsync(id, request);

        if (user == null)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "User not found.",
                "USER_NOT_FOUND");
        }

        return Success(user, "User updated successfully.");
    }

    [HttpDelete("{id}")]
    public async Task<GenericApiResponse<object?>> DeleteUser(string id)
    {
        var deleted =
            await _userService.DeleteAsync(id);

        if (!deleted)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "User not found.",
                "USER_NOT_FOUND");
        }

        return Success(null, "User deleted successfully.");
    }
}