namespace ProjectFlow.Api.Authorization;

public static class Permissions
{
    public const string ViewDashboard = "viewDashboard";

    public const string ViewProjects = "viewProjects";
    public const string CreateProject = "createProject";
    public const string EditProject = "editProject";
    public const string DeleteProject = "deleteProject";

    public const string ViewTasks = "viewTasks";
    public const string CreateTask = "createTask";
    public const string EditTask = "editTask";
    public const string DeleteTask = "deleteTask";
    public const string AssignTask = "assignTask";

    public const string ViewTeam = "viewTeam";
    public const string ManageTeam = "manageTeam";

    public const string ViewAnalytics = "viewAnalytics";

    public const string UseAIPlanner = "useAIPlanner";

    public const string ManageUsers = "manageUsers";
    public const string ManageRoles = "manageRoles";
    public const string SystemSettings = "systemSettings";

    public const string SubmitWork = "submitWork";
    public const string ReviewWork = "reviewWork";

    public static IReadOnlyList<string> All { get; } =
    [
        ViewDashboard,
        ViewProjects,
        CreateProject,
        EditProject,
        DeleteProject,
        ViewTasks,
        CreateTask,
        EditTask,
        DeleteTask,
        AssignTask,
        ViewTeam,
        ManageTeam,
        ViewAnalytics,
        UseAIPlanner,
        ManageUsers,
        ManageRoles,
        SystemSettings,
        SubmitWork,
        ReviewWork
    ];
}