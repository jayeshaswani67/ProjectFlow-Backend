using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProjectFlow.Api.Models;

public class AppSettings
{
    [BsonId]
    public string Id { get; set; } = "system";

    public string ApplicationName { get; set; }
        = "ProjectFlow";

    public string CompanyName { get; set; }
        = "";

    public string Timezone { get; set; }
        = "Asia/Kolkata";

    public string DefaultProjectStatus { get; set; }
        = "Planning";

    public string DefaultTaskPriority { get; set; }
        = "Medium";

    public bool AllowEmployeeCreateProjects { get; set; }

    public bool AllowEmployeeCreateTasks { get; set; }
        = true;

    public bool RequireTaskAssignment { get; set; }
        = true;

    public bool EmailNotifications { get; set; }
        = true;

    public bool TaskNotifications { get; set; }
        = true;

    public bool ProjectNotifications { get; set; }
        = true;

    public DateTime UpdatedAt { get; set; }
}