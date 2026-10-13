
namespace ProjectFlow.Api.DTOs;

public class CreateTeamMemberRequest
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "Member";

    public string Department { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";
}

public class UpdateTeamMemberRequest
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "Member";

    public string Department { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";
}

public class TeamMemberResponse
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
