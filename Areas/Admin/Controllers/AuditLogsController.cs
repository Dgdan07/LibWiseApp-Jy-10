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

    public async Task<IActionResult> Index(string search)
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

        ViewBag.Search = search;
        return View(await logs.OrderByDescending(l => l.Timestamp).Take(200).ToListAsync());
    }
}
