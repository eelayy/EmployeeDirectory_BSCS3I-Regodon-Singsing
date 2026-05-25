using System.ComponentModel.DataAnnotations;

namespace EmployeeDirectory.Models;

/// <summary>
/// Stores OTP issuance and verification attempts for account security workflows.
/// </summary>
public class OtpVerification
{
    [Key]
    public int OtpVerificationId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(20)]
    public string OtpCode { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public int AttemptCount { get; set; }

    public bool IsLocked { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Generated";
}
