using System.ComponentModel.DataAnnotations;

namespace EmployeeDirectory.ViewModels;

public class ProfileViewModel
{
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "User name")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Photo URL")]
    public string? PhotoUrl { get; set; }

    [Display(Name = "Upload photo")]
    public Microsoft.AspNetCore.Http.IFormFile? PhotoFile { get; set; }
}
