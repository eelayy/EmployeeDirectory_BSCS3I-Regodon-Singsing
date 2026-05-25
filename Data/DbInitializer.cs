using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EmployeeDirectory.Models;

namespace EmployeeDirectory.Data;

/// <summary>
/// Seeds initial roles, users, departments, and sample employees.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Ensure database is created, applies migrations, and seeds data.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var context = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        // Apply pending migrations
        await context.Database.MigrateAsync();

        // Seed roles
        string[] roles = new[] { "Admin", "HR", "Employee" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed admin user
        var adminEmail = "admin@company.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true,
                LastLoginAt = null
            };
            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
        else if (!admin.IsActive)
        {
            admin.IsActive = true;
            await userManager.UpdateAsync(admin);
        }

        // Seed departments
        if (!context.Departments.Any())
        {
            var engineering = new Department { Name = "Engineering", Description = "Engineering Department" };
            var hr = new Department { Name = "HR", Description = "Human Resources" };
            var marketing = new Department { Name = "Marketing", Description = "Marketing Department" };

            context.Departments.AddRange(engineering, hr, marketing);
            await context.SaveChangesAsync();

            // Seed sample employees
            if (!context.Employees.Any())
            {
                var employees = new List<Employee>
                {
                    new Employee { FirstName = "Alice", LastName = "Johnson", Email = "alice.johnson@company.com", JobTitle = "Senior Engineer", DepartmentId = engineering.DepartmentId, HireDate = DateTime.UtcNow.AddYears(-3) },
                    new Employee { FirstName = "Bob", LastName = "Smith", Email = "bob.smith@company.com", JobTitle = "Engineer", DepartmentId = engineering.DepartmentId, HireDate = DateTime.UtcNow.AddYears(-2) },
                    new Employee { FirstName = "Carol", LastName = "Taylor", Email = "carol.taylor@company.com", JobTitle = "HR Manager", DepartmentId = hr.DepartmentId, HireDate = DateTime.UtcNow.AddYears(-5) },
                    new Employee { FirstName = "David", LastName = "Brown", Email = "david.brown@company.com", JobTitle = "Recruiter", DepartmentId = hr.DepartmentId, HireDate = DateTime.UtcNow.AddYears(-1) },
                    new Employee { FirstName = "Eve", LastName = "Davis", Email = "eve.davis@company.com", JobTitle = "Marketing Lead", DepartmentId = marketing.DepartmentId, HireDate = DateTime.UtcNow.AddYears(-4) },
                    new Employee { FirstName = "Frank", LastName = "Miller", Email = "frank.miller@company.com", JobTitle = "Marketing Specialist", DepartmentId = marketing.DepartmentId, HireDate = DateTime.UtcNow.AddYears(-2) },
                    new Employee { FirstName = "Grace", LastName = "Wilson", Email = "grace.wilson@company.com", JobTitle = "Engineer", DepartmentId = engineering.DepartmentId, HireDate = DateTime.UtcNow.AddYears(-1) },
                    new Employee { FirstName = "Hank", LastName = "Moore", Email = "hank.moore@company.com", JobTitle = "Engineer", DepartmentId = engineering.DepartmentId, HireDate = DateTime.UtcNow.AddMonths(-6) },
                    new Employee { FirstName = "Ivy", LastName = "Clark", Email = "ivy.clark@company.com", JobTitle = "HR Coordinator", DepartmentId = hr.DepartmentId, HireDate = DateTime.UtcNow.AddYears(-1) },
                    new Employee { FirstName = "Jack", LastName = "Lopez", Email = "jack.lopez@company.com", JobTitle = "Engineer", DepartmentId = engineering.DepartmentId, HireDate = DateTime.UtcNow.AddMonths(-2) }
                };

                context.Employees.AddRange(employees);
                await context.SaveChangesAsync();

                if (!context.AuditLogs.Any())
                {
                    context.AuditLogs.Add(new AuditLog
                    {
                        ActingUserId = admin?.Id ?? string.Empty,
                        ActionType = "SeedData",
                        TargetEntityType = "System",
                        TargetEntityId = "Initialization",
                        Description = "Initial sample data seeded for dashboard bootstrap.",
                        Timestamp = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
