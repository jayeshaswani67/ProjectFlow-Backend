using ProjectFlow.Api.DTOs;

namespace ProjectFlow.Api.Authorization;

public interface IPermissionService
{
    IReadOnlyCollection<string> GetPermissions(string role);
    UserPermissions GetUserPermissions(string role);
}

public sealed class PermissionService : IPermissionService
{
    public IReadOnlyCollection<string> GetPermissions(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return Array.Empty<string>();
        }

        return role.Trim().ToUpperInvariant() switch
        {
            "ADMIN" => Permissions.All,
            "MANAGER" =>
            [
                Permissions.ViewDashboard,
                Permissions.ViewProjects,
                Permissions.CreateProject,
                Permissions.EditProject,
                Permissions.DeleteProject,
                Permissions.ViewTasks,
                Permissions.CreateTask,
                Permissions.EditTask,
                Permissions.DeleteTask,
                Permissions.AssignTask,
                Permissions.ViewTeam,
                Permissions.ManageTeam,
                Permissions.ViewAnalytics,
                Permissions.UseAIPlanner,
                Permissions.SubmitWork,
                Permissions.ReviewWork
            ],
            "EMPLOYEE" =>
            [
                Permissions.ViewDashboard,
                Permissions.ViewProjects,
                Permissions.ViewTasks,
                Permissions.CreateTask,
                Permissions.EditTask,
                Permissions.ViewTeam,
                Permissions.UseAIPlanner,
                Permissions.SubmitWork
            ],
            "DEVELOPER" =>
            [
                Permissions.ViewProjects,
                Permissions.ViewTasks,
                Permissions.CreateTask,
                Permissions.EditTask,
                Permissions.ViewTeam,
                Permissions.UseAIPlanner,
                Permissions.SubmitWork
            ],
            _ => Array.Empty<string>()
        };
    }

    public UserPermissions GetUserPermissions(string role)
    {
        var permissions = GetPermissions(role);
        return new UserPermissions
        {
            ViewDashboard = permissions.Contains(Permissions.ViewDashboard),
            ViewProjects = permissions.Contains(Permissions.ViewProjects),
            CreateProject = permissions.Contains(Permissions.CreateProject),
            EditProject = permissions.Contains(Permissions.EditProject),
            DeleteProject = permissions.Contains(Permissions.DeleteProject),
            ViewTasks = permissions.Contains(Permissions.ViewTasks),
            CreateTask = permissions.Contains(Permissions.CreateTask),
            EditTask = permissions.Contains(Permissions.EditTask),
            DeleteTask = permissions.Contains(Permissions.DeleteTask),
            AssignTask = permissions.Contains(Permissions.AssignTask),
            ViewTeam = permissions.Contains(Permissions.ViewTeam),
            ManageTeam = permissions.Contains(Permissions.ManageTeam),
            ViewAnalytics = permissions.Contains(Permissions.ViewAnalytics),
            UseAIPlanner = permissions.Contains(Permissions.UseAIPlanner),
            ManageUsers = permissions.Contains(Permissions.ManageUsers),
            ManageRoles = permissions.Contains(Permissions.ManageRoles),
            SystemSettings = permissions.Contains(Permissions.SystemSettings),
            SubmitWork = permissions.Contains(Permissions.SubmitWork),
            ReviewWork = permissions.Contains(Permissions.ReviewWork)
        };
    }
}