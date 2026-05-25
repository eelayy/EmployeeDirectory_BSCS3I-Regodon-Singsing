using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeDirectory.Data;
using EmployeeDirectory.Models;

namespace EmployeeDirectory.Controllers;

/// <summary>
/// Manages employee directory CRUD and listing.
/// </summary>
[Authorize]
public class EmployeeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<EmployeeDirectory.Hubs.EmployeeHub> _hubContext;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EmployeeController(ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<EmployeeDirectory.Hubs.EmployeeHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Shows paginated list of employees.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var employees = await _context.Employees.Include(e => e.Department).ToListAsync();
        return View(employees);
    }

    /// <summary>
    /// Employee details view.
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var employee = await _context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.EmployeeId == id);
        if (employee == null) return NotFound();
        return View(employee);
    }

    /// <summary>
    /// Create form.
    /// </summary>
    [Authorize(Roles = "Admin,HR")]
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Create POST handler.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Create(Employee employee, Microsoft.AspNetCore.Http.IFormFile? photoFile)
    {
        if (!ModelState.IsValid) return View(employee);

        // Handle photo file upload
        if (photoFile != null && photoFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext) || photoFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(string.Empty, "Invalid image file. Use JPG/PNG and keep under 2MB.");
                return View(employee);
            }
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var filePath = Path.Combine(uploads, fileName);
            using (var fs = System.IO.File.Create(filePath))
            {
                await photoFile.CopyToAsync(fs);
            }
            employee.PhotoUrl = "/uploads/" + fileName;
        }

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Employee created.";
        await _hubContext.Clients.All.SendCoreAsync("EmployeeChanged", new object[] { }, default);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Edit form.
    /// </summary>
    [Authorize(Roles = "Admin,HR,Employee")]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound();
        return View(employee);
    }

    /// <summary>
    /// Edit POST.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,HR,Employee")]
    public async Task<IActionResult> Edit(int id, Employee employee, Microsoft.AspNetCore.Http.IFormFile? photoFile)
    {
        if (id != employee.EmployeeId) return BadRequest();
        if (!ModelState.IsValid) return View(employee);

        var existing = await _context.Employees.FindAsync(id);
        if (existing == null) return NotFound();

        existing.FirstName = employee.FirstName;
        existing.LastName = employee.LastName;
        existing.Email = employee.Email;
        existing.Phone = employee.Phone;
        existing.JobTitle = employee.JobTitle;
        existing.DepartmentId = employee.DepartmentId;
        existing.HireDate = employee.HireDate;
        existing.IsActive = employee.IsActive;

        // Handle photo replacement
        if (photoFile != null && photoFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext) || photoFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(string.Empty, "Invalid image file. Use JPG/PNG and keep under 2MB.");
                return View(employee);
            }
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var filePath = Path.Combine(uploads, fileName);
            using (var fs = System.IO.File.Create(filePath))
            {
                await photoFile.CopyToAsync(fs);
            }
            existing.PhotoUrl = "/uploads/" + fileName;
        }

        _context.Update(existing);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Employee updated.";
        await _hubContext.Clients.All.SendCoreAsync("EmployeeChanged", new object[] { }, default);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Delete confirmation.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound();
        return View(employee);
    }

    /// <summary>
    /// Delete POST.
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Employee deleted.";
            await _hubContext.Clients.All.SendCoreAsync("EmployeeChanged", new object[] { }, default);
        }
        return RedirectToAction(nameof(Index));
    }
}
