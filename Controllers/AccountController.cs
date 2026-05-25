using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeDirectory.Data;
using EmployeeDirectory.Models;
using EmployeeDirectory.ViewModels;

namespace EmployeeDirectory.Controllers;

/// <summary>
/// Lightweight Account controller to forward to Identity UI pages where needed.
/// </summary>
public class AccountController : Controller
{
    private const int OtpMaxAttempts = 5;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _environment = environment;
    }

    /// <summary>
    /// Shows the current user's profile for viewing and light editing.
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var model = new ProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            UserName = user.UserName ?? string.Empty
        };

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
        if (employee != null)
        {
            model.FirstName = employee.FirstName;
            model.LastName = employee.LastName;
            model.PhotoUrl = employee.PhotoUrl;
        }

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (model.PhoneNumber != user.PhoneNumber)
        {
            var setPhone = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
            if (!setPhone.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Unable to update phone number.");
                return View(model);
            }
        }

        // Update or create associated employee record
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
        if (employee == null)
        {
            employee = new EmployeeDirectory.Models.Employee
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = model.FirstName,
                LastName = model.LastName,
                HireDate = DateTime.UtcNow,
                IsActive = true
            };
            _context.Employees.Add(employee);
        }
        else
        {
            employee.FirstName = model.FirstName;
            employee.LastName = model.LastName;
        }

        // Handle photo upload
        if (model.PhotoFile != null && model.PhotoFile.Length > 0)
        {
            var uploads = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            var ext = Path.GetExtension(model.PhotoFile.FileName);
            var fileName = user.Id + ext;
            var filePath = Path.Combine(uploads, fileName);
            using (var fs = System.IO.File.Create(filePath))
            {
                await model.PhotoFile.CopyToAsync(fs);
            }
            employee.PhotoUrl = "/uploads/" + fileName;
        }

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction("Profile");
    }

    /// <summary>
    /// Show the login page.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Process a login attempt.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account is disabled or does not exist.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            var otpCode = GenerateOtpCode();
            _context.OtpVerifications.Add(new OtpVerification
            {
                UserId = user.Id,
                OtpCode = otpCode,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                AttemptCount = 0,
                IsLocked = false,
                Status = "Generated"
            });
            await _context.SaveChangesAsync();

            if (_environment.IsDevelopment())
            {
                TempData["DevelopmentOtpCode"] = otpCode;
            }

            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(VerifyOtp), new
            {
                pendingUserId = user.Id,
                model.ReturnUrl
            });
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    /// <summary>
    /// Show the registration page.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Process a registration attempt.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Employee");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return await RedirectByRoleAsync(user, model.ReturnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    /// <summary>
    /// Sign the current user out.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect(returnUrl ?? Url.Content("~/"));
    }

    /// <summary>
    /// Shows an access denied page for unauthorized routes.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// Shows the OTP verification page.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult VerifyOtp(string pendingUserId, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(pendingUserId))
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new OtpVerifyViewModel
        {
            PendingUserId = pendingUserId,
            ReturnUrl = returnUrl
        });
    }

    /// <summary>
    /// Processes OTP verification and finalizes sign-in.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(OtpVerifyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.PendingUserId);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Unable to verify OTP for this account.");
            return View(model);
        }

        var otpRecord = await _context.OtpVerifications
            .Where(o => o.UserId == user.Id && !o.IsUsed)
            .OrderByDescending(o => o.GeneratedAt)
            .FirstOrDefaultAsync();

        if (otpRecord == null)
        {
            ModelState.AddModelError(string.Empty, "No OTP request found. Please login again.");
            return View(model);
        }

        if (otpRecord.IsLocked)
        {
            ModelState.AddModelError(string.Empty, "Account OTP verification is locked. Contact an administrator.");
            return View(model);
        }

        if (otpRecord.ExpiresAt < DateTime.UtcNow)
        {
            otpRecord.Status = "Expired";
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, "OTP expired. Please login again.");
            return View(model);
        }

        if (!string.Equals(otpRecord.OtpCode, model.Code, StringComparison.Ordinal))
        {
            otpRecord.AttemptCount += 1;
            otpRecord.Status = "Failed";

            if (otpRecord.AttemptCount >= OtpMaxAttempts)
            {
                otpRecord.IsLocked = true;
                otpRecord.Status = "Locked";
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

                _context.AuditLogs.Add(new AuditLog
                {
                    ActingUserId = user.Id,
                    ActionType = "OtpLocked",
                    TargetEntityType = "User",
                    TargetEntityId = user.Id,
                    Description = $"OTP lockout triggered for user {user.Email}.",
                    Timestamp = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);
            ModelState.AddModelError(string.Empty, "Invalid OTP code.");
            return View(model);
        }

        otpRecord.IsUsed = true;
        otpRecord.Status = "Used";
        user.LastLoginAt = DateTime.UtcNow;
        user.LockoutEnd = null;

        _context.AuditLogs.Add(new AuditLog
        {
            ActingUserId = user.Id,
            ActionType = "OtpVerified",
            TargetEntityType = "User",
            TargetEntityId = user.Id,
            Description = $"OTP verification completed for user {user.Email}.",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await _userManager.UpdateAsync(user);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return await RedirectByRoleAsync(user, model.ReturnUrl);
    }

    private async Task<IActionResult> RedirectByRoleAsync(ApplicationUser user, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        return RedirectToAction("Index", "Home");
    }

    private static string GenerateOtpCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
