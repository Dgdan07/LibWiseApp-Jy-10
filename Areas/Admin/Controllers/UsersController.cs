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

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
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

        return View(userViewModels);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Role = model.Role,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            await _auditLog.LogAsync("Create", "User", user.Id, $"Created user \"{model.UserName}\" with role {model.Role}");
            TempData["Success"] = "User created successfully.";
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var model = new RegisterViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
            Role = roles.FirstOrDefault() ?? "AssistantLibrarian"
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, RegisterViewModel model)
    {
        if (id != model.Id) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.UserName;
        user.Role = model.Role;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, model.Role);

        await _auditLog.LogAsync("Update", "User", user.Id, $"Updated user \"{model.UserName}\" to role {model.Role}");
        TempData["Success"] = "User updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.UserName == "admin")
        {
            TempData["Error"] = "Cannot delete the admin account.";
            return RedirectToAction("Index");
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            await _auditLog.LogAsync("Delete", "User", id, $"Deleted user \"{user.UserName}\"");
            TempData["Success"] = "User deleted.";
        }
        else
        {
            TempData["Error"] = "Failed to delete user.";
        }

        return RedirectToAction("Index");
    }
}
