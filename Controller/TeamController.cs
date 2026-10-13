
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/team")]
[Authorize(Policy = Permissions.ViewTeam)]
public class TeamController : GenericApiControllerBase
{
    private readonly TeamService _teamService;

    public TeamController(
        TeamService teamService)
    {
        _teamService = teamService;
    }

    private string UserId =>
        User.FindFirstValue(
            ClaimTypes.NameIdentifier
        )
        ?? User.FindFirstValue("sub")
        ?? string.Empty;


    // GET /api/team
    [HttpGet]
    public async Task<GenericApiResponse<object?>> GetTeamMembers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var validation = ValidatePagination(page, pageSize);
        if (validation != null)
        {
            return validation;
        }

        var result =
            await _teamService
                .GetTeamMembersAsync(UserId, page, pageSize);

        return Success(
            result.Members,
            "Team members retrieved successfully.",
            meta: BuildPaginationMeta(page, pageSize, result.TotalCount)
        );
    }

    private GenericApiResponse<object?>? ValidatePagination(int page, int pageSize)
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
            totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }


    // GET /api/team/{id}
    [HttpGet("{id}")]
    public async Task<GenericApiResponse<object?>> GetTeamMember(
        string id)
    {
        var member =
            await _teamService
                .GetTeamMemberAsync(
                    id,
                    UserId
                );

        if (member == null)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "Team member not found.",
                "TEAM_MEMBER_NOT_FOUND"
            );
        }

        return Success(member, "Team member retrieved successfully.");
    }


    // POST /api/team
    [HttpPost]
    [Authorize(Policy = Permissions.ManageTeam)]
    public async Task<GenericApiResponse<object?>> CreateTeamMember(
        [FromBody]
        CreateTeamMemberRequest request)
    {
        try
        {
            var member =
                await _teamService
                    .CreateTeamMemberAsync(
                        request,
                        UserId
                    );

            Response.Headers.Location =
                Url.Action(nameof(GetTeamMember), new { id = member.Id })
                ?? $"/api/team/{member.Id}";

            return Success(
                member,
                "Team member created successfully.",
                StatusCodes.Status201Created
            );
        }
        catch (ArgumentException ex)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                ex.Message,
                "INVALID_TEAM_MEMBER"
            );
        }
    }


    // PUT /api/team/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.ManageTeam)]
    public async Task<GenericApiResponse<object?>> UpdateTeamMember(
        string id,
        [FromBody]
        UpdateTeamMemberRequest request)
    {
        try
        {
            var member =
                await _teamService
                    .UpdateTeamMemberAsync(
                        id,
                        request,
                        UserId
                    );

            if (member == null)
            {
                return Failure(
                    StatusCodes.Status404NotFound,
                    "Team member not found.",
                    "TEAM_MEMBER_NOT_FOUND"
                );
            }

            return Success(member, "Team member updated successfully.");
        }
        catch (ArgumentException ex)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                ex.Message,
                "INVALID_TEAM_MEMBER"
            );
        }
    }


    // DELETE /api/team/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.ManageTeam)]
    public async Task<GenericApiResponse<object?>> DeleteTeamMember(
        string id)
    {
        var deleted =
            await _teamService
                .DeleteTeamMemberAsync(
                    id,
                    UserId
                );

        if (!deleted)
        {
            return Failure(
                StatusCodes.Status404NotFound,
                "Team member not found.",
                "TEAM_MEMBER_NOT_FOUND"
            );
        }

        return Success(null, "Team member deleted successfully.");
    }
}
