using MongoDB.Driver;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Interfaces;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public sealed class ProfileService(MongoDbContext db) : IUserService
{
    public async Task<ProfileDto?> GetProfileAsync(string userId)
    {
        var user = await db.Users
            .Find(user => user.Id == userId)
            .FirstOrDefaultAsync();

        return user is null ? null : MapProfile(user);
    }

    public async Task<ProfileDto?> UpdateProfileAsync(
        string userId,
        string name)
    {
        var update = Builders<User>.Update
            .Set(user => user.Name, name.Trim())
            .Set(user => user.UpdatedAt, DateTime.UtcNow);

        var result = await db.Users.UpdateOneAsync(
            user => user.Id == userId,
            update
        );

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return await GetProfileAsync(userId);
    }

    private static ProfileDto MapProfile(User user)
    {
        return new ProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsActive = string.Equals(
                user.Status,
                "Active",
                StringComparison.OrdinalIgnoreCase
            ),
            CreatedAt = user.CreatedAt
        };
    }
}
