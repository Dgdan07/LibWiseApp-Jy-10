using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;

    public CategoriesController(AppDbContext db, AuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _db.Categories.OrderBy(c => c.Name).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Category name is required.";
            return RedirectToAction("Index");
        }

        _db.Categories.Add(new Category { Name = name.Trim(), Description = description?.Trim() });
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Create", "Category", "", $"Added category \"{name.Trim()}\"");
        TempData["Success"] = "Category created.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, string name, string? description)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Category name is required.";
            return RedirectToAction("Index");
        }

        cat.Name = name.Trim();
        cat.Description = description?.Trim();
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Update", "Category", id.ToString(), $"Updated category to \"{name.Trim()}\"");
        TempData["Success"] = "Category updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Delete", "Category", id.ToString(), $"Deleted category \"{cat.Name}\"");
        TempData["Success"] = "Category deleted.";
        return RedirectToAction("Index");
    }
}
