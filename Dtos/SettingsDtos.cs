namespace ProjectFlow.Api.DTOs;

public sealed record SettingsResponse(
	string ApplicationName,
	string CompanyName,
	string Timezone,
	string DefaultProjectStatus,
	string DefaultTaskPriority,
	bool AllowEmployeeCreateProjects,
	bool AllowEmployeeCreateTasks,
	bool RequireTaskAssignment,
	bool EmailNotifications,
	bool TaskNotifications,
	bool ProjectNotifications,
	DateTime UpdatedAt);

public sealed record UpdateSettingsRequest(
	string ApplicationName,
	string CompanyName,
	string Timezone,
	string DefaultProjectStatus,
	string DefaultTaskPriority,
	bool AllowEmployeeCreateProjects,
	bool AllowEmployeeCreateTasks,
	bool RequireTaskAssignment,
	bool EmailNotifications,
	bool TaskNotifications,
	bool ProjectNotifications);
