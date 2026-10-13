
namespace ProjectFlow.Api.DTOs;

public sealed class ProfileDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Backend role: Admin, Manager, or Developer.
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}

public sealed class UpdateProfileDto
{
    public string Name { get; set; } = string.Empty;
}
