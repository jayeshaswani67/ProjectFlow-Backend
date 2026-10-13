using System.ComponentModel.DataAnnotations;

namespace ProjectFlow.Api.DTOs;

public class CreateUserRequest
{
    [Required]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    public string? Role { get; set; }

    public string JobRole { get; set; } = "Developer";

    public string Department { get; set; } = "";

    public string Status { get; set; } = "Active";
}

public class UpdateUserRequest
{
    public string Name { get; set; } = "";

    public string Email { get; set; } = "";

    public string? Role { get; set; }

    public string JobRole { get; set; } = "Developer";

    public string Department { get; set; } = "";

    public string Status { get; set; } = "Active";
}
