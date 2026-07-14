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

    private const int PageSize = 10;

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _db.Categories.OrderBy(c => c.Name);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, error = "Category name is required." });

        _db.Categories.Add(new Category { Name = name.Trim(), Description = description?.Trim() });
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Create", "Category", "", $"Added category \"{name.Trim()}\"");
        return Json(new { success = true, message = "Category created." });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null)
            return Json(new { success = false, message = "Category not found." });

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Delete", "Category", id.ToString(), $"Deleted category \"{cat.Name}\"");
        return Json(new { success = true, message = "Category deleted." });
    }
}
