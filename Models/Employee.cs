using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeDirectory.Models;

/// <summary>
/// Represents an employee in the directory.
/// </summary>
public class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? JobTitle { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    public DateTime HireDate { get; set; }

    [Url]
    public string? PhotoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(2000)]
    public string? InternalNotes { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}
