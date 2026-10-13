using MongoDB.Driver;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Exceptions;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public class UserService
{
    private readonly MongoDbContext _db;

    public UserService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserResponse>> GetAllAsync()
    {
        var users = await _db.Users
            .Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();

        return users.Select(Map).ToList();
    }

    public async Task<UserResponse?> GetByIdAsync(string id)
    {
        var user = await _db.Users
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();

        return user == null ? null : Map(user);
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var existingUser = await _db.Users
            .Find(x => x.Email.ToLower() == email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "USER_ALREADY_EXISTS",
                "A user with this email already exists."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "PASSWORD_REQUIRED",
                "A password is required to create a login account."
            );
        }

        var normalizedRole = NormalizeRole(request.Role ?? request.JobRole);

        if (normalizedRole == null)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "INVALID_ROLE",
                "Invalid role."
            );
        }

        var user = new User
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId()
                .ToString(),

            Name = request.Name.Trim(),

            Email = email,

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.Password
                ),

            Role = normalizedRole,

            JobRole = request.JobRole?.Trim() ?? "",
            Department = request.Department.Trim(),

            Status = request.Status,

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow
        };

        await _db.Users.InsertOneAsync(user);

        return Map(user);
    }

    public async Task<UserResponse?> UpdateAsync(
        string id,
        UpdateUserRequest request)
    {
        var user = await _db.Users
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        var normalizedRole = request.Role is null
            ? user.Role
            : NormalizeRole(request.Role);

        if (normalizedRole == null)
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "INVALID_ROLE",
                "Invalid role."
            );
        }

        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var duplicateUser = await _db.Users
            .Find(x => x.Email.ToLower() == email && x.Id != id)
            .FirstOrDefaultAsync();

        if (duplicateUser != null)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "USER_ALREADY_EXISTS",
                "A user with this email already exists."
            );
        }

        user.Name = request.Name.Trim();
        user.Email = email;
        user.Role = normalizedRole;
        user.JobRole = request.JobRole.Trim();

        user.Department = request.Department.Trim();
        user.Status = request.Status;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.Users.ReplaceOneAsync(
            x => x.Id == id,
            user
        );

        return Map(user);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _db.Users.DeleteOneAsync(
            x => x.Id == id
        );

        return result.DeletedCount > 0;
    }

    public async Task<bool> SetStatusAsync(
        string id,
        string status)
    {
        var update = Builders<User>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _db.Users.UpdateOneAsync(
            x => x.Id == id,
            update
        );

        return result.ModifiedCount > 0;
    }

    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return role.Trim().ToLowerInvariant() switch
        {
            "admin" => "Admin",
            "manager" => "Manager",
            "employee" => "Employee",
            _ => null
        };
    }

    private static UserResponse Map(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            JobRole = user.JobRole,
            Department = user.Department,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}