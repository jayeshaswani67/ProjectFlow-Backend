using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProjectFlow.Api.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("Role")]
    public string Role { get; set; } = "Employee";
    public string JobRole { get; set; } = "Developer";
    public string Department { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string? Avtaar { get; set; }

    public string? ResetPasswordToken { get; set;}
    public DateTime? ResetPasswordTokenExpiry { get; set;}

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
}