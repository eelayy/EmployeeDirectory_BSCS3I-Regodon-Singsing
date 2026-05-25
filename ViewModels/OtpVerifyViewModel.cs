using System.ComponentModel.DataAnnotations;

namespace EmployeeDirectory.ViewModels;

/// <summary>
/// Captures OTP verification input after successful username/password authentication.
/// </summary>
public class OtpVerifyViewModel
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    public string PendingUserId { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
