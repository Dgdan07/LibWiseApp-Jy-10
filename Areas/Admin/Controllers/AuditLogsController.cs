using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : Controller
{
    private readonly AppDbContext _db;

    public AuditLogsController(AppDbContext db) => _db = db;

    private const int PageSize = 10;

    public async Task<IActionResult> Index(string search, string? actionType, int page = 1)
    {
        var logs = _db.AuditLogs
            .Include(l => l.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            logs = logs.Where(l =>
                l.Action.Contains(search) ||
                l.EntityType.Contains(search) ||
                l.Details!.Contains(search) ||
                l.User!.UserName!.Contains(search));

        if (!string.IsNullOrWhiteSpace(actionType))
            logs = logs.Where(l => l.Action == actionType);

        var total = await logs.CountAsync();
        var items = await logs
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.ActionType = actionType;
        ViewBag.ActionTypes = await _db.AuditLogs.Select(l => l.Action).Distinct().OrderBy(a => a).ToListAsync();
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        return View(items);
    }
}
