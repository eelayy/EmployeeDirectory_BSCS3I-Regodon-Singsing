using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeDirectory.Data;
using EmployeeDirectory.Models;
using EmployeeDirectory.ViewModels;

namespace EmployeeDirectory.Controllers;

/// <summary>
/// Admin operations: user management and department management.
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<EmployeeDirectory.Hubs.EmployeeHub> _hubContext;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IWebHostEnvironment env,
        Microsoft.AspNetCore.SignalR.IHubContext<EmployeeDirectory.Hubs.EmployeeHub> hubContext)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
        _env = env;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Displays the admin dashboard with KPI cards, chart data, and recent activity.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = startOfMonth.AddMonths(-1);
        var recentActivityCount = _configuration.GetValue<int>("Dashboard:RecentActivityCount", 10);
        var chartMonthsBack = _configuration.GetValue<int>("Dashboard:ChartMonthsBack", 6);

        var activeEmployees = await _context.Employees.CountAsync(e => e.IsActive);
        var previousActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive && e.HireDate < lastMonthStart);
        var trendDelta = activeEmployees - previousActiveEmployees;
        var trendDirection = trendDelta >= 0 ? "up" : "down";

        var departments = await _context.Departments
            .Select(d => new DepartmentSummaryViewModel
            {
                DepartmentName = d.Name,
                EmployeeCount = d.Employees != null ? d.Employees.Count : 0
            })
            .ToListAsync();

        var roleDistribution = new Dictionary<string, int>
        {
            ["Admin"] = 0,
            ["HR"] = 0,
            ["Employee"] = 0
        };

        foreach (var role in roleDistribution.Keys.ToArray())
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            roleDistribution[role] = usersInRole.Count;
        }

        var monthlyHires = new List<HiresSeriesPointViewModel>();
        for (var i = chartMonthsBack - 1; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            monthlyHires.Add(new HiresSeriesPointViewModel
            {
                Label = monthStart.ToString("MMM yyyy"),
                Count = await _context.Employees.CountAsync(e => e.HireDate >= monthStart && e.HireDate < monthEnd)
            });
        }

        var activities = await _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.ActingUser)
            .OrderByDescending(a => a.Timestamp)
            .Take(recentActivityCount)
            .Select(a => new RecentActivityViewModel
            {
                ActionType = a.ActionType,
                Description = a.Description,
                Timestamp = a.Timestamp,
                ActingUserEmail = a.ActingUser != null ? a.ActingUser.Email : null
            })
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            TotalEmployees = activeEmployees,
            TotalDepartments = await _context.Departments.CountAsync(),
            NewHiresThisMonth = await _context.Employees.CountAsync(e => e.HireDate >= startOfMonth),
            InactiveEmployees = await _context.Employees.CountAsync(e => !e.IsActive),
            EmployeeTrendDelta = trendDelta,
            EmployeeTrendDirection = trendDirection,
            EmployeesPerDepartment = departments,
            RoleDistribution = roleDistribution,
            NewHireSeries = monthlyHires,
            RecentActivity = activities
        };

        return View(model);
    }

    /// <summary>
    /// Lists all registered users for management.
    /// </summary>
    [HttpGet]
    public IActionResult UserManagement()
    {
        var users = _userManager.Users.ToList();
        return View(users);
    }

    /// <summary>
    /// Performs a global employee search for dashboard navbar usage.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchEmployees(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Json(AdminJsonResponse.Ok("No search term provided.", Array.Empty<object>()));
        }

        var normalizedTerm = term.Trim();
        var matches = await _context.Employees
            .AsNoTracking()
            .Where(e =>
                EF.Functions.Like(e.FirstName, $"%{normalizedTerm}%") ||
                EF.Functions.Like(e.LastName, $"%{normalizedTerm}%") ||
                EF.Functions.Like(e.Email, $"%{normalizedTerm}%") ||
                (e.JobTitle != null && EF.Functions.Like(e.JobTitle, $"%{normalizedTerm}%")))
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Take(10)
            .Select(e => new
            {
                e.EmployeeId,
                Name = e.FirstName + " " + e.LastName,
                e.Email,
                e.JobTitle
            })
            .ToListAsync();

        return Json(AdminJsonResponse.Ok("Search results loaded.", matches));
    }

    /// <summary>
    /// Updates selected employee fields inline without leaving the grid.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickEditEmployee(int employeeId, string field, string value)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        if (employee == null)
        {
            return Json(AdminJsonResponse.Fail("Employee not found."));
        }

        var normalizedField = field.Trim().ToLowerInvariant();
        switch (normalizedField)
        {
            case "name":
                var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    return Json(AdminJsonResponse.Fail("Name value is invalid."));
                }
                employee.FirstName = parts[0];
                employee.LastName = parts.Length > 1 ? parts[1] : string.Empty;
                break;
            case "jobtitle":
            case "title":
                employee.JobTitle = value;
                break;
            case "department":
                if (!int.TryParse(value, out var departmentId))
                {
                    return Json(AdminJsonResponse.Fail("Department value must be a valid id."));
                }
                var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentId == departmentId);
                if (!departmentExists)
                {
                    return Json(AdminJsonResponse.Fail("Department not found."));
                }
                employee.DepartmentId = departmentId;
                break;
            default:
                return Json(AdminJsonResponse.Fail("Unsupported field for quick edit."));
        }

        await _context.SaveChangesAsync();
        await LogAuditAsync("EmployeeUpdated", "Employee", employee.EmployeeId.ToString(), $"Quick edit updated field '{field}' for employee {employee.Email}.");
        await _hubContext.Clients.All.SendCoreAsync("EmployeeChanged", new object[] { }, default);
        return Json(AdminJsonResponse.Ok("Employee updated successfully."));
    }

    /// <summary>
    /// Bulk deactivates selected employees.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeactivate([FromBody] List<int> employeeIds)
    {
        if (employeeIds.Count == 0)
        {
            return Json(AdminJsonResponse.Fail("No employees selected."));
        }

        var employees = await _context.Employees.Where(e => employeeIds.Contains(e.EmployeeId)).ToListAsync();
        foreach (var employee in employees)
        {
            employee.IsActive = false;
        }

        await _context.SaveChangesAsync();
        await LogAuditAsync("EmployeeBulkDeactivated", "Employee", string.Join(',', employeeIds), $"Bulk deactivated {employees.Count} employees.");
        await _hubContext.Clients.All.SendCoreAsync("EmployeeChanged", new object[] { }, default);
        return Json(AdminJsonResponse.Ok($"Deactivated {employees.Count} employees."));
    }

    /// <summary>
    /// Bulk deletes selected employees.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete([FromBody] List<int> employeeIds)
    {
        if (employeeIds.Count == 0)
        {
            return Json(AdminJsonResponse.Fail("No employees selected."));
        }

        var employees = await _context.Employees.Where(e => employeeIds.Contains(e.EmployeeId)).ToListAsync();
        _context.Employees.RemoveRange(employees);
        await _context.SaveChangesAsync();
        await LogAuditAsync("EmployeeBulkDeleted", "Employee", string.Join(',', employeeIds), $"Bulk deleted {employees.Count} employees.");
        await _hubContext.Clients.All.SendCoreAsync("EmployeeChanged", new object[] { }, default);
        return Json(AdminJsonResponse.Ok($"Deleted {employees.Count} employees."));
    }

    /// <summary>
    /// Updates an individual user role from the user management table.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Json(AdminJsonResponse.Fail("User not found."));
        }

        var validRoles = new[] { "Admin", "HR", "Employee" };
        if (!validRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return Json(AdminJsonResponse.Fail("Role is invalid."));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return Json(AdminJsonResponse.Fail("Failed removing current role."));
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            return Json(AdminJsonResponse.Fail("Failed assigning new role."));
        }

        await LogAuditAsync("RoleChanged", "User", user.Id, $"Changed role for {user.Email} to {role}.");
        return Json(AdminJsonResponse.Ok("User role updated."));
    }

    /// <summary>
    /// Disables an account by applying a long-running lockout.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Json(AdminJsonResponse.Fail("User not found."));
        }

        user.IsActive = false;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Json(AdminJsonResponse.Fail("Unable to disable user."));
        }

        await LogAuditAsync("AccountDisabled", "User", user.Id, $"Disabled account for user {user.Email}.");
        return Json(AdminJsonResponse.Ok("Account disabled."));
    }

    /// <summary>
    /// Resets OTP lockout and attempts for a selected user.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetOtpLockout(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Json(AdminJsonResponse.Fail("User not found."));
        }

        user.LockoutEnd = null;
        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        var otpRows = await _context.OtpVerifications.Where(o => o.UserId == userId).ToListAsync();
        foreach (var otp in otpRows)
        {
            otp.AttemptCount = 0;
            otp.IsLocked = false;
            if (otp.Status == "Locked")
            {
                otp.Status = "Reset";
            }
        }

        await _context.SaveChangesAsync();
        await LogAuditAsync("OtpReset", "User", user.Id, $"Reset OTP lockout for user {user.Email}.");
        return Json(AdminJsonResponse.Ok("OTP lockout reset."));
    }

    /// <summary>
    /// Removes used and expired OTP records older than 30 days.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CleanOtpLogs()
    {
        var threshold = DateTime.UtcNow.AddDays(-30);
        var expiredLogs = await _context.OtpVerifications
            .Where(o => o.IsUsed && o.ExpiresAt < threshold)
            .ToListAsync();

        _context.OtpVerifications.RemoveRange(expiredLogs);
        await _context.SaveChangesAsync();

        await LogAuditAsync("OtpLogsCleaned", "OtpVerification", "ExpiredLogs", $"Cleared {expiredLogs.Count} expired OTP logs.");
        await _hubContext.Clients.All.SendCoreAsync("OtpLogsCleaned", new object[] { }, default);
        return Json(AdminJsonResponse.Ok($"Cleared {expiredLogs.Count} expired logs."));
    }

    /// <summary>
    /// Displays the department management section scaffold.
    /// </summary>
    [HttpGet]
    public IActionResult Departments()
    {
        ViewData["SectionTitle"] = "Departments";
        var departments = _context.Departments.AsNoTracking().OrderBy(d => d.Name).ToList();
        return View(departments);
    }

    [HttpGet]
    public IActionResult CreateDepartment()
    {
        ViewData["SectionTitle"] = "Create Department";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepartment(Department dept)
    {
        if (!ModelState.IsValid)
        {
            return View(dept);
        }

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();
        await LogAuditAsync("DepartmentCreated", "Department", dept.DepartmentId.ToString(), $"Created department {dept.Name}");
        await _hubContext.Clients.All.SendCoreAsync("DepartmentChanged", new object[] { }, default);
        return RedirectToAction(nameof(Departments));
    }

    [HttpGet]
    public async Task<IActionResult> EditDepartment(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return NotFound();
        return View(dept);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDepartment(Department dept)
    {
        if (!ModelState.IsValid) return View(dept);
        _context.Departments.Update(dept);
        await _context.SaveChangesAsync();
        await LogAuditAsync("DepartmentUpdated", "Department", dept.DepartmentId.ToString(), $"Updated department {dept.Name}");
        await _hubContext.Clients.All.SendCoreAsync("DepartmentChanged", new object[] { }, default);
        return RedirectToAction(nameof(Departments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return Json(AdminJsonResponse.Fail("Department not found."));
        _context.Departments.Remove(dept);
        await _context.SaveChangesAsync();
        await LogAuditAsync("DepartmentDeleted", "Department", id.ToString(), $"Deleted department {dept.Name}");
        await _hubContext.Clients.All.SendCoreAsync("DepartmentChanged", new object[] { }, default);
        return Json(AdminJsonResponse.Ok("Department deleted."));
    }

    /// <summary>
    /// Displays the security logs section scaffold.
    /// </summary>
    [HttpGet]
    public IActionResult SecurityLogs()
    {
        ViewData["SectionTitle"] = "OTP / Security Logs";
        var logs = _context.OtpVerifications
            .AsNoTracking()
            .OrderByDescending(o => o.GeneratedAt)
            .Take(500)
            .ToList();
        return View(logs);
    }

    /// <summary>
    /// Displays the reports and export section scaffold.
    /// </summary>
    [HttpGet]
    public IActionResult Reports()
    {
        ViewData["SectionTitle"] = "Reports & Export";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportEmployees(string format = "csv")
    {
        var employees = await _context.Employees.AsNoTracking().ToListAsync();
        if (format == "csv")
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("EmployeeId,FirstName,LastName,Email,JobTitle,DepartmentId,IsActive");
            foreach (var e in employees)
            {
                sb.AppendLine($"{e.EmployeeId},\"{e.FirstName}\",\"{e.LastName}\",\"{e.Email}\",\"{e.JobTitle}\",{e.DepartmentId},{e.IsActive}");
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "employees.csv");
        }

        return BadRequest("Unsupported export format.");
    }

    /// <summary>
    /// Displays the settings section scaffold.
    /// </summary>
    [HttpGet]
    public IActionResult Settings()
    {
        ViewData["SectionTitle"] = "Settings";
        var model = new Dictionary<string, string?>
        {
            ["Dashboard:RecentActivityCount"] = _configuration["Dashboard:RecentActivityCount"],
            ["Dashboard:ChartMonthsBack"] = _configuration["Dashboard:ChartMonthsBack"]
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings([FromForm] int recentActivityCount, [FromForm] int chartMonthsBack)
    {
        // Writes minimal keys to appsettings.json in content root. Not suitable for production without precautions.
        var path = System.IO.Path.Combine(_env.ContentRootPath, "appsettings.json");
        if (!System.IO.File.Exists(path)) return Json(AdminJsonResponse.Fail("appsettings.json not found."));

        var json = await System.IO.File.ReadAllTextAsync(path);
        var jdoc = System.Text.Json.JsonDocument.Parse(json);
        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        // Build a mutable object
        var root = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
        if (!root.ContainsKey("Dashboard")) root["Dashboard"] = new Dictionary<string, object?>();

        var dashboard = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(System.Text.Json.JsonSerializer.Serialize(root["Dashboard" ])) ?? new();
        dashboard["RecentActivityCount"] = recentActivityCount;
        dashboard["ChartMonthsBack"] = chartMonthsBack;
        root["Dashboard"] = dashboard;

        var outJson = System.Text.Json.JsonSerializer.Serialize(root, options);
        await System.IO.File.WriteAllTextAsync(path, outJson);

        await LogAuditAsync("SettingsUpdated", "Settings", "Dashboard", $"Updated Dashboard settings: RecentActivityCount={recentActivityCount}, ChartMonthsBack={chartMonthsBack}");
        return Json(AdminJsonResponse.Ok("Settings saved. Restart may be required for changes to take effect."));
    }

    private async Task LogAuditAsync(string actionType, string targetEntityType, string targetEntityId, string description)
    {
        var actingUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actingUserId))
        {
            return;
        }

        _context.AuditLogs.Add(new AuditLog
        {
            ActingUserId = actingUserId,
            ActionType = actionType,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            Description = description,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
