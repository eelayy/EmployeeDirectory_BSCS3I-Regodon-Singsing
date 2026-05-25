using Microsoft.AspNetCore.Identity;

namespace EmployeeDirectory.Models;

/// <summary>
/// Application user extending IdentityUser.
/// </summary>
public class ApplicationUser : IdentityUser
{
	/// <summary>
	/// Last successful OTP verification timestamp in UTC.
	/// </summary>
	public DateTime? LastLoginAt { get; set; }

	/// <summary>
	/// Whether the account is active for sign-in.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>
	/// Internal admin-only notes for user management.
	/// </summary>
	public string? InternalNotes { get; set; }
}
