using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EmployeeDirectory.Models;
using EmployeeDirectory.Data;
using Microsoft.EntityFrameworkCore;

using System.Linq;

namespace EmployeeDirectory.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var employees = await _context.Employees.Include(e => e.Department).Where(e => e.IsActive).ToListAsync();
        return View(employees);
    }

    public async Task<IActionResult> EmployeesPartial()
    {
        var employees = await _context.Employees.Include(e => e.Department).Where(e => e.IsActive).ToListAsync();
        return PartialView("_EmployeesList", employees);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
