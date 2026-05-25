using System.ComponentModel.DataAnnotations;

namespace EmployeeDirectory.Models;

/// <summary>
/// Captures auditable actions performed by users in the admin area.
/// </summary>
public class AuditLog
{
    [Key]
    public int AuditId { get; set; }

    [Required]
    public string ActingUserId { get; set; } = string.Empty;

    public ApplicationUser? ActingUser { get; set; }

    [Required]
    [MaxLength(100)]
    public string ActionType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TargetEntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TargetEntityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
