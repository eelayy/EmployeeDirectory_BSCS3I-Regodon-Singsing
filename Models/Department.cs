using System.ComponentModel.DataAnnotations;

namespace EmployeeDirectory.Models;

/// <summary>
/// Represents a Department.
/// </summary>
public class Department
{
    [Key]
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? HeadEmployeeId { get; set; }
    public Employee? Head { get; set; }

    public ICollection<Employee>? Employees { get; set; }
}
