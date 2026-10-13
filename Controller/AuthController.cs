using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.Configuration;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.DTOs.Auth;
using ProjectFlow.Api.Helpers;
using ProjectFlow.Api.Services;

namespace ProjectFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : GenericApiControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IPermissionService _permissionService;
    private readonly JwtSettings _jwtSettings;
    private readonly bool _isProduction;

    public AuthController(
        AuthService authService,
        ILogger<AuthController> logger,
        IPermissionService permissionService,
        JwtSettings jwtSettings,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _logger = logger;
        _permissionService = permissionService;
        _jwtSettings = jwtSettings;
        _isProduction = environment.IsProduction();
    }

    
    // LOGIN
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<GenericApiResponse<object?>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "LOGIN REQUEST RECEIVED. Email: {Email}",
            request.Email
        );

        try
        {  
            // VALIDATE REQUEST
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning(
                    "LOGIN FAILED: Email or password is empty."
                );

                return Failure(
                    StatusCodes.Status400BadRequest,
                    "Email and password are required.",
                    "CREDENTIALS_REQUIRED");
            }

            
            // AUTHENTICATE USER
            var result =
                await _authService.LoginAsync(
                    request,
                    cancellationToken);

            
            // INVALID CREDENTIALS / ROLE
            if (result == null)
            {
                _logger.LogWarning(
                    "LOGIN FAILED: Invalid credentials or role. Email: {Email}",
                    request.Email
                );

                return Failure(
                    StatusCodes.Status401Unauthorized,
                    "Invalid email or password.",
                    "INVALID_CREDENTIALS");
            }

            
            // SUCCESS
            _logger.LogInformation(
                "LOGIN SUCCESSFUL. User: {UserId}, Role: {Role}",
                result.User.Id,
                result.User.Role
            );

            AuthCookieHelper.SetAuthCookies(
                Response,
                result.AccessToken,
                result.RefreshToken,
                _jwtSettings.ExpirationMinutes,
                _jwtSettings.RefreshTokenExpirationDays,
                _isProduction
            );

            return Success(
                new
                {
                    accessToken = result.AccessToken,
                    user = result.User,
                    expiresIn = result.ExpiresIn
                },
                "Login successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "LOGIN EXCEPTION. Email: {Email}",
                request.Email
            );

            return Failure(
                StatusCodes.Status500InternalServerError,
                "Internal server error during login.",
                "LOGIN_FAILED"
            );
        }
    }

    
    // REFRESH TOKEN
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<GenericApiResponse<object?>> Refresh(
        [FromBody] RefreshTokenRequest? request)
    {
        _logger.LogInformation(
            "REFRESH TOKEN REQUEST RECEIVED."
        );

        try
        {
            
            // VALIDATE REQUEST
            var refreshToken =
                Request.Cookies[
                    AuthCookieHelper.RefreshTokenCookie
                ];

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                refreshToken = request?.RefreshToken;
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning(
                    "REFRESH FAILED: Refresh token is empty."
                );

                return Failure(
                    StatusCodes.Status400BadRequest,
                    "Refresh token is required.",
                    "REFRESH_TOKEN_REQUIRED");
            }

            
            // REFRESH TOKEN
            var result =
                await _authService.RefreshAsync(
                    refreshToken
                );

            
            // INVALID TOKEN
            if (result == null)
            {
                _logger.LogWarning(
                    "REFRESH FAILED: Invalid or expired refresh token."
                );

                return Failure(
                    StatusCodes.Status401Unauthorized,
                    "Invalid or expired refresh token.",
                    "INVALID_REFRESH_TOKEN");
            }

            
            // SUCCESS    
            _logger.LogInformation(
                "TOKEN REFRESH SUCCESSFUL. User: {UserId}, Role: {Role}",
                result.User.Id,
                result.User.Role
            );

            AuthCookieHelper.SetAuthCookies(
                Response,
                result.AccessToken,
                result.RefreshToken,
                _jwtSettings.ExpirationMinutes,
                _jwtSettings.RefreshTokenExpirationDays,
                _isProduction
            );

            return Success(
                new { user = result.User, expiresIn = result.ExpiresIn },
                "Token refresh successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "REFRESH TOKEN EXCEPTION."
            );

            return Failure(
                StatusCodes.Status500InternalServerError,
                "Internal server error during token refresh.",
                "TOKEN_REFRESH_FAILED"
            );
        }
    }

    

    // ============================================================
// FORGOT PASSWORD
// ============================================================

[HttpPost("forgot-password")]
[AllowAnonymous]
public async Task<GenericApiResponse<object?>> ForgotPassword(
    [FromBody] ForgotPasswordRequest request,
    CancellationToken cancellationToken)
{
    _logger.LogInformation(
        "FORGOT PASSWORD REQUEST RECEIVED. Email: {Email}",
        request.Email
    );

    try
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Email is required.",
                "EMAIL_REQUIRED");
        }

        await _authService.ForgotPasswordAsync(
            request.Email,
            cancellationToken
        );

        // Always return the same response.
        // This prevents revealing whether an account exists.
        return Success(
            null,
            "If an account exists for this email, a password reset link has been sent.");
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "FORGOT PASSWORD EXCEPTION. Email: {Email}",
            request.Email
        );

        return Failure(
            StatusCodes.Status500InternalServerError,
            "Unable to process password reset request.",
            "FORGOT_PASSWORD_FAILED"
        );
    }
}

// ============================================================
// RESET PASSWORD
// ============================================================

[HttpPost("reset-password")]
[AllowAnonymous]
public async Task<GenericApiResponse<object?>> ResetPassword(
    [FromBody] ResetPasswordRequest request,
    CancellationToken cancellationToken)
{
    _logger.LogInformation(
        "RESET PASSWORD REQUEST RECEIVED."
    );

    try
    {
        if (request == null)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Invalid request.",
                "INVALID_REQUEST");
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Reset token is required.",
                "RESET_TOKEN_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "New password is required.",
                "NEW_PASSWORD_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(
            request.ConfirmPassword))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Confirm password is required.",
                "CONFIRM_PASSWORD_REQUIRED");
        }

        if (request.NewPassword !=
            request.ConfirmPassword)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Passwords do not match.",
                "PASSWORD_MISMATCH");
        }

        if (request.NewPassword.Length < 8)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Password must be at least 8 characters long.",
                "PASSWORD_TOO_SHORT");
        }

        var success =
            await _authService.ResetPasswordAsync(
                request.Token,
                request.NewPassword,
                request.ConfirmPassword,
                cancellationToken
            );

        if (!success)
        {
            _logger.LogWarning(
                "RESET PASSWORD FAILED: Invalid or expired token."
            );

            return Failure(
                StatusCodes.Status400BadRequest,
                "The password reset link is invalid or has expired.",
                "INVALID_RESET_TOKEN");
        }

        _logger.LogInformation(
            "PASSWORD RESET SUCCESSFUL."
        );

        return Success(
            null,
            "Password reset successfully. You can now login with your new password.");
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "RESET PASSWORD EXCEPTION."
        );

        return Failure(
            StatusCodes.Status500InternalServerError,
            "Unable to reset password.",
            "PASSWORD_RESET_FAILED"
        );
    }
}

    
    // CURRENT USER
    

    [Authorize]
    [HttpGet("me")]
    public GenericApiResponse<object?> Me()
    {
        var userId =
            GetCurrentUserId();

        var name =
            User.FindFirstValue(
                ClaimTypes.Name
            );

        var email =
            User.FindFirstValue(
                ClaimTypes.Email
            );

        var role =
            GetCurrentUserRole();

       
        // REQUIRED JWT CLAIMS
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(role))
        {
            _logger.LogWarning(
                "ME FAILED: Required claims missing."
            );

            return Failure(
                StatusCodes.Status401Unauthorized,
                "Invalid authentication token.",
                "INVALID_AUTHENTICATION_TOKEN");
        }

       
        // VALIDATE ROLE
       

        if (!IsValidRole(role))
        {
            _logger.LogWarning(
                "ME FAILED: Invalid role {Role}. User: {UserId}",
                role,
                userId
            );

            return Failure(
                StatusCodes.Status403Forbidden,
                "The authenticated user has an invalid role.",
                "INVALID_USER_ROLE");
        }

       
        // GET PERMISSIONS
        var permissions =
            _permissionService.GetUserPermissions(role);

       
        // RESPONSE
       

        return Success(
            new { id = userId, name, email, role, permissions },
            "User profile retrieved successfully.");
    }

    
    // CURRENT USER ID
    

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(
                   ClaimTypes.NameIdentifier
               )
               ?? User.FindFirstValue(
                   JwtRegisteredClaimNames.Sub
               )
               ?? User.FindFirstValue("sub");
    }

    
    // CURRENT USER ROLE
    

    private string? GetCurrentUserRole()
    {
        return User.FindFirstValue(
            ClaimTypes.Role
        );
    }

    
    // ROLE VALIDATION
    

    private static bool IsValidRole(
        string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return
            string.Equals(
                role,
                "Admin",
                StringComparison.OrdinalIgnoreCase
            )
            ||
            string.Equals(
                role,
                "Manager",
                StringComparison.OrdinalIgnoreCase
            )
            ||
            string.Equals(
                role,
                "Employee",
                StringComparison.OrdinalIgnoreCase
            );
    }

}
