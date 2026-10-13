using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProjectFlow.Api.Models;

public class Project
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public string Status { get; set; } = "Active";

    public string Priority { get; set; } = "Medium";
    
    public int Progress { get; set; } = 0;

    public int TotalTasks { get; set; } = 0;

    public int CompletedTasks { get; set; } = 0;

    public List<string> TeamMembers { get; set; } = new();

    public DateTime DueDate { get; set; } 
    public string CreatedBy { get; set; } = "";
    public string? ManagerId { get; set; }
    public string? TeamId { get; set; } 
    public List<string> MemberIds { get; set; } =  [];     

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}