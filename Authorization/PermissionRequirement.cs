using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ProjectFlow.Api.Authorization;

public sealed record PermissionRequirement(string Permission)
    : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(
    IPermissionService permissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        if (role is not null &&
            permissionService.GetPermissions(role).Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
