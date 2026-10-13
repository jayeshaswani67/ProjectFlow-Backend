using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

using ProjectFlow.Api.Configuration;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public class AuthService
{
    private readonly MongoDbContext _db;
    private readonly JwtSettings _jwtSettings;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AuthService> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly string[] AllowedRoles =
    {
        "Admin",
        "Manager",
        "Employee"
    };

    public AuthService(
        MongoDbContext db,
        JwtSettings jwtSettings,
        IPermissionService permissionService,
        ILogger<AuthService> logger,
        IHostEnvironment environment)
    {
        _db = db;
        _jwtSettings = jwtSettings;
        _permissionService = permissionService;
        _logger = logger;
        _environment = environment;
    }

    // 
    // LOGIN
    // 
public async Task<LoginResponse?> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken = default)
{
    Console.WriteLine();
    Console.WriteLine("AUTH SERVICE: LOGIN");

    try
    {
        // 1. Validate request
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            Console.WriteLine(
                "LOGIN FAILED: Email or password empty.");

            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();

        // 2. Find user
        var user = await _db.Users
            .Find(x => x.Email.ToLower() == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            Console.WriteLine("LOGIN FAILED: User not found.");
            return null;
        }

        // 3. Validate role
        var normalizedRole = NormalizeRole(user.Role);

        if (normalizedRole == null)
        {
            Console.WriteLine(
                $"LOGIN FAILED: Invalid role: {user.Role}");

            return null;
        }

        user.Role = normalizedRole;

        // 4. Verify password
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            Console.WriteLine("LOGIN FAILED: Password hash missing.");
            return null;
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user.PasswordHash
        );

        if (!passwordValid)
        {
            Console.WriteLine("LOGIN FAILED: Invalid password.");
            return null;
        }

        Console.WriteLine("Credentials verified.");

        // 5. Generate short-lived access token
        var accessToken = GenerateAccessToken(user);

        // 6. Generate refresh token
        var refreshToken = GenerateRefreshToken();

        var now = DateTime.UtcNow;

        var refreshTokenDocument = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = now,
            ExpiresAt = now.AddDays(
                _jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        // 7. Store refresh token
        await _db.RefreshTokens.InsertOneAsync(
            refreshTokenDocument,
            cancellationToken: cancellationToken
        );

        // 8. Return login response
        Console.WriteLine("LOGIN SUCCESS");

        return BuildLoginResponse(
            user,
            accessToken,
            refreshToken
        );
    }
    catch (Exception ex)
    {
        // Avoid logging passwords or token values.
        _logger.LogError(
            ex,
            "An error occurred while authenticating a user.");

        throw;
    }
}


    // 
    // REFRESH TOKEN
    // 

    public async Task<LoginResponse?> RefreshAsync(
        string refreshToken)
    {
        Console.WriteLine();
        Console.WriteLine("AUTH SERVICE: REFRESH");

        try
        {
            // -------------------------------------------------
            // Validate input
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                Console.WriteLine(
                    "REFRESH FAILED: Token empty."
                );

                return null;
            }

            refreshToken = refreshToken.Trim();

            Console.WriteLine(
                $"Refresh token length: {refreshToken.Length}"
            );

            // -------------------------------------------------
            // Find active refresh token
            // -------------------------------------------------

            var storedToken =
                await _db.RefreshTokens
                    .Find(x =>
                        x.Token == refreshToken &&
                        !x.IsRevoked &&
                        x.ExpiresAt > DateTime.UtcNow
                    )
                    .FirstOrDefaultAsync();

            if (storedToken == null)
            {
                Console.WriteLine(
                    "REFRESH FAILED: Token not found, expired, or revoked."
                );

                return null;
            }

            Console.WriteLine(
                $"Refresh token found: {storedToken.Id}"
            );

            Console.WriteLine(
                $"User ID: {storedToken.UserId}"
            );

            Console.WriteLine(
                $"Expires: {storedToken.ExpiresAt:O}"
            );
            // -------------------------------------------------
            // Find user
            // -------------------------------------------------

            var user =
                await _db.Users
                    .Find(x =>
                        x.Id == storedToken.UserId
                    )
                    .FirstOrDefaultAsync();

            if (user == null)
            {
                Console.WriteLine(
                    "REFRESH FAILED: User not found."
                );

                return null;
            }

            // -------------------------------------------------
            // Validate role
            // -------------------------------------------------

            var normalizedRole =
                NormalizeRole(user.Role);

            if (normalizedRole == null)
            {
                Console.WriteLine(
                    $"REFRESH FAILED: Invalid role: {user.Role}"
                );

                return null;
            }

            user.Role = normalizedRole;

            // -------------------------------------------------
            // Generate new tokens FIRST
            // -------------------------------------------------

            var newAccessToken =
                GenerateAccessToken(user);

            var newRefreshToken =
                GenerateRefreshToken();

            Console.WriteLine(
                "New access token generated."
            );

            Console.WriteLine(
                "New refresh token generated."
            );

            // -------------------------------------------------
            // Revoke old refresh token
            // -------------------------------------------------

            storedToken.IsRevoked = true;

            await _db.RefreshTokens.ReplaceOneAsync(
                x => x.Id == storedToken.Id,
                storedToken
            );

            Console.WriteLine(
                "Old refresh token revoked."
            );

            // -------------------------------------------------
            // Save new refresh token
            // -------------------------------------------------

            var newRefreshTokenEntity =
                new RefreshToken
                {
                    UserId = user.Id,

                    Token = newRefreshToken,

                    ExpiresAt =
                        DateTime.UtcNow.AddDays(
                            _jwtSettings.RefreshTokenExpirationDays
                        ),

                    IsRevoked = false,

                    CreatedAt = DateTime.UtcNow
                };

            await _db.RefreshTokens.InsertOneAsync(
                newRefreshTokenEntity
            );

            Console.WriteLine(
                "New refresh token saved."
            );

            Console.WriteLine(
                "REFRESH SUCCESS"
            );

            return BuildLoginResponse(
                user,
                newAccessToken,
                newRefreshToken
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine(
                " REFRESH EXCEPTION "
            );

            Console.WriteLine(
                $"Type: {ex.GetType().FullName}"
            );

            Console.WriteLine(
                $"Message: {ex.Message}"
            );

            Console.WriteLine(
                $"StackTrace: {ex.StackTrace}"
            );

            Console.WriteLine(
                ""
            );

            throw;
        }
    }
    // 
    // GENERATE ACCESS TOKEN
    // 

    private string GenerateAccessToken(
        User user)
    {
        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    user.Id
                ),

                new(
                    ClaimTypes.Name,
                    user.Name
                ),

                new(
                    ClaimTypes.Email,
                    user.Email
                ),

                new(
                    ClaimTypes.Role,
                    user.Role
                ),

                new(
                    "job_role",
                    user.JobRole
                )
            };

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _jwtSettings.SecretKey
                )
            );

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

        var expiration =
            DateTime.UtcNow.AddMinutes(
                _jwtSettings.ExpirationMinutes
            );

        var token =
            new JwtSecurityToken(
                issuer:
                    _jwtSettings.Issuer,

                audience:
                    _jwtSettings.Audience,

                claims:
                    claims,

                expires:
                    expiration,

                signingCredentials:
                    credentials
            );

        Console.WriteLine(
            $"Access token expires UTC: {expiration:O}"
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    // 
    // GENERATE REFRESH TOKEN
    // 

    private static string GenerateRefreshToken()
    {
        var randomBytes =
            RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(
            randomBytes
        );
    }

    // 
    // NORMALIZE ROLE
    // 

    private static string? NormalizeRole(
        string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        var normalizedRole =
            role.Trim();

        foreach (var allowedRole in AllowedRoles)
        {
            if (string.Equals(
                allowedRole,
                normalizedRole,
                StringComparison.OrdinalIgnoreCase))
            {
                return allowedRole;
            }
        }

        return null;
    }

    // 
    // BUILD LOGIN RESPONSE
    // 

    private LoginResponse BuildLoginResponse(
        User user,
        string accessToken,
        string refreshToken)
    {
        return new LoginResponse
        {
            AccessToken =
                accessToken,

            RefreshToken =
                refreshToken,

            ExpiresIn =
                _jwtSettings
                    .ExpirationMinutes * 60,

            User =
                new UserResponse
                {
                    Id =
                        user.Id,

                    Name =
                        user.Name,

                    Email =
                        user.Email,

                    Role =
                        user.Role,

                    JobRole =
                        user.JobRole,

                    Permissions =
                        _permissionService.GetUserPermissions(user.Role)
                }
        };
    }
    public async Task<bool> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Find(x => x.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return true;
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var resetToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        user.ResetPasswordToken = resetToken;
        user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(20);

        await _db.Users.ReplaceOneAsync(
            x => x.Id == user.Id,
            user,
            cancellationToken: cancellationToken);

        if (_environment.IsDevelopment())
        {
            Console.WriteLine(
                $"[Development] PASSWORD RESET TOKEN for {user.Email}: {resetToken} " +
                $"(expires at {user.ResetPasswordTokenExpiry:O})");
        }

        return true;
    }

public async Task<bool> ResetPasswordAsync(
    string token,
    string newPassword,
    string confirmPassword,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(token))
    {
        return false;
    }

    if (string.IsNullOrWhiteSpace(newPassword))
    {
        return false;
    }

    if (newPassword != confirmPassword)
    {
        return false;
    }

    if (newPassword.Length < 8)
    {
        return false;
    }

    var user = await _db.Users
        .Find(x =>
            x.ResetPasswordToken == token &&
            x.ResetPasswordTokenExpiry != null &&
            x.ResetPasswordTokenExpiry > DateTime.UtcNow
        )
        .FirstOrDefaultAsync(cancellationToken);

    if (user == null)
    {
        return false;
    }

    // Hash the new password using BCrypt.
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
        newPassword
    );

    // Invalidate the token immediately.
    user.ResetPasswordToken = null;
    user.ResetPasswordTokenExpiry = null;

    await _db.Users.ReplaceOneAsync(
        x => x.Id == user.Id,
        user,
        cancellationToken: cancellationToken
    );

    return true;
}

    internal async Task<bool> ResetPasswordAsync(object token, object newPassword, object confirmPassword)
    {
        throw new NotImplementedException();
    }

    internal async Task<bool> ResetPasswordAsync(object token, string newPassword, object confirmPassword)
    {
        throw new NotImplementedException();
    }

    internal async Task LoginAsync(Microsoft.AspNetCore.Identity.Data.LoginRequest request)
    {
        throw new NotImplementedException();
    }
}
