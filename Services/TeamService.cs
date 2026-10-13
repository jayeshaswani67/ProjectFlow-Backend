
using MongoDB.Driver;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public class TeamService
{
    private readonly MongoDbContext _context;

    public TeamService(MongoDbContext context)
    {
        _context = context;
    }

    // GET ALL TEAM MEMBERS
    public async Task<(List<TeamMemberResponse> Members, long TotalCount)> GetTeamMembersAsync(
        string ownerId,
        int page,
        int pageSize)
    {
        var filter = Builders<TeamMember>.Filter.Eq(
            member => member.OwnerId,
            ownerId);
        var totalCount = await _context.TeamMembers.CountDocumentsAsync(filter);
        var skip = ((long)page - 1) * pageSize;
        if (skip > int.MaxValue)
        {
            return ([], totalCount);
        }

        var members = await _context.TeamMembers
            .Find(filter)
            .SortByDescending(member => member.JoinedAt)
            .ThenBy(member => member.Id)
            .Skip((int)skip)
            .Limit(pageSize)
            .ToListAsync();

        return (
            members.Select(MapToResponse).ToList(),
            totalCount
        );
    }

    // GET TEAM MEMBER BY ID
    public async Task<TeamMemberResponse?> GetTeamMemberAsync(
        string id,
        string ownerId)
    {
        var member = await _context.TeamMembers
            .Find(
                member =>
                    member.Id == id &&
                    member.OwnerId == ownerId
            )
            .FirstOrDefaultAsync();

        if (member == null)
        {
            return null;
        }

        return MapToResponse(member);
    }

    // CREATE TEAM MEMBER
    public async Task<TeamMemberResponse> CreateTeamMemberAsync(
        CreateTeamMemberRequest request,
        string ownerId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Team member name is required."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException(
                "Email is required."
            );
        }

        // Prevent duplicate email for same owner
        var existingMember =
            await _context.TeamMembers
                .Find(
                    member =>
                        member.OwnerId == ownerId &&
                        member.Email.ToLower() ==
                        request.Email.Trim().ToLower()
                )
                .FirstOrDefaultAsync();

        if (existingMember != null)
        {
            throw new ArgumentException(
                "A team member with this email already exists."
            );
        }

        var member = new TeamMember
        {
            Name = request.Name.Trim(),

            Email = request.Email
                .Trim()
                .ToLower(),

            Role = string.IsNullOrWhiteSpace(request.Role)
                ? "Member"
                : request.Role.Trim(),

            Department = request.Department?.Trim() ?? "",

            Avatar = request.Avatar?.Trim() ?? "",

            Status = string.IsNullOrWhiteSpace(request.Status)
                ? "Active"
                : request.Status.Trim(),

            OwnerId = ownerId,

            JoinedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow
        };

        await _context.TeamMembers.InsertOneAsync(member);

        return MapToResponse(member);
    }

    // UPDATE TEAM MEMBER
    public async Task<TeamMemberResponse?> UpdateTeamMemberAsync(
        string id,
        UpdateTeamMemberRequest request,
        string ownerId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Team member name is required."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException(
                "Email is required."
            );
        }

        var existingMember =
            await _context.TeamMembers
                .Find(
                    member =>
                        member.Id == id &&
                        member.OwnerId == ownerId
                )
                .FirstOrDefaultAsync();

        if (existingMember == null)
        {
            return null;
        }

        // Check duplicate email
        var duplicateEmail =
            await _context.TeamMembers
                .Find(
                    member =>
                        member.OwnerId == ownerId &&
                        member.Id != id &&
                        member.Email.ToLower() ==
                        request.Email.Trim().ToLower()
                )
                .FirstOrDefaultAsync();

        if (duplicateEmail != null)
        {
            throw new ArgumentException(
                "Another team member already uses this email."
            );
        }

        existingMember.Name =
            request.Name.Trim();

        existingMember.Email =
            request.Email.Trim().ToLower();

        existingMember.Role =
            string.IsNullOrWhiteSpace(request.Role)
                ? "Member"
                : request.Role.Trim();

        existingMember.Department =
            request.Department?.Trim() ?? "";

        existingMember.Avatar =
            request.Avatar?.Trim() ?? "";

        existingMember.Status =
            string.IsNullOrWhiteSpace(request.Status)
                ? "Active"
                : request.Status.Trim();

        existingMember.UpdatedAt =
            DateTime.UtcNow;

        await _context.TeamMembers.ReplaceOneAsync(
            member =>
                member.Id == id &&
                member.OwnerId == ownerId,
            existingMember
        );

        return MapToResponse(existingMember);
    }

    // DELETE TEAM MEMBER
    public async Task<bool> DeleteTeamMemberAsync(
        string id,
        string ownerId)
    {
        var result =
            await _context.TeamMembers.DeleteOneAsync(
                member =>
                    member.Id == id &&
                    member.OwnerId == ownerId
            );

        return result.DeletedCount > 0;
    }

    // MAP MODEL → RESPONSE
    private static TeamMemberResponse MapToResponse(
        TeamMember member)
    {
        return new TeamMemberResponse
        {
            Id = member.Id,

            Name = member.Name,

            Email = member.Email,

            Role = member.Role,

            Department = member.Department,

            Avatar = member.Avatar,

            Status = member.Status,

            JoinedAt = member.JoinedAt,

            UpdatedAt = member.UpdatedAt
        };
    }
}
