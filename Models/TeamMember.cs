
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProjectFlow.Api.Models;

public class TeamMember
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "Member";

    public string Department { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

    public string OwnerId { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
