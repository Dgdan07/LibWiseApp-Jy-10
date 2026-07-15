using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;
using LibWiseApp.Services;
using LibWiseApp.ViewModels;

namespace LibWiseApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AuditLogService _auditLog;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, AuditLogService auditLog)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditLog = auditLog;
    }

    private const int PageSize = 10;

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _userManager.Users.OrderBy(u => u.UserName);
        var total = await query.CountAsync();
        var users = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var userViewModels = new List<RegisterViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new RegisterViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                Role = roles.FirstOrDefault() ?? "AssistantLibrarian"
            });
        }

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        return View(userViewModels);
    }

    [HttpGet]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Json(new
        {
            id = user.Id,
            firstName = user.FirstName,
            lastName = user.LastName,
            email = user.Email ?? "",
            userName = user.UserName ?? "",
            role = roles.FirstOrDefault() ?? "AssistantLibrarian"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return Json(new { success = false, error = errors });
        }

        if (model.Role == "Admin")
        {
            var existingAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            if (existingAdmins.Any())
                return Json(new { success = false, error = "An admin account already exists. Only one admin is allowed." });
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            await _auditLog.LogAsync("Create", "User", user.Id, $"Created user \"{model.UserName}\" with role {model.Role}");
            return Json(new { success = true, message = "User created successfully." });
        }

        return Json(new { success = false, error = string.Join(" ", result.Errors.Select(e => e.Description)) });
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromForm] RegisterViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Id))
            return Json(new { success = false, error = "User ID is required." });

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return Json(new { success = false, error = "User not found." });

        if (model.Role == "Admin")
        {
            var existingAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            if (existingAdmins.Any(a => a.Id != user.Id))
                return Json(new { success = false, error = "An admin account already exists. Only one admin is allowed." });
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.UserName;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Json(new { success = false, error = string.Join(" ", result.Errors.Select(e => e.Description)) });

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, model.Role);

        await _auditLog.LogAsync("Update", "User", user.Id, $"Updated user \"{model.UserName}\" to role {model.Role}");
        return Json(new { success = true, message = "User updated successfully." });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return Json(new { success = false, message = "User not found." });

        if (user.Email == "admin@libwise.com")
            return Json(new { success = false, message = "Cannot delete the admin account." });

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            await _auditLog.LogAsync("Delete", "User", id, $"Deleted user \"{user.UserName}\"");
            return Json(new { success = true, message = "User deleted." });
        }

        return Json(new { success = false, message = "Failed to delete user." });
    }
}
