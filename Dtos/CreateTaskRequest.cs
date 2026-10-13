
using System.ComponentModel.DataAnnotations;

namespace ProjectFlow.Api.DTOs;

public class CreateTaskRequest
{
    [Required]
    [MinLength(1)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string Priority { get; set; } = "Medium";

    public string AssignedTo { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }
}
