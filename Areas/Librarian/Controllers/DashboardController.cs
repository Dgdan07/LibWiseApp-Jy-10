using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.Librarian.Controllers;

[Area("Librarian")]
[Authorize(Roles = "Admin,Librarian")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? days)
    {
        var now = DateTime.UtcNow;
        DateTime? from = days.HasValue ? now.AddDays(-days.Value) : null;
        DateTime? to = days.HasValue ? now : null;
        ViewBag.Days = days;

        ViewBag.TotalBooks = await _db.Books.CountAsync();
        ViewBag.AvailableBooks = await _db.Books.SumAsync(b => b.AvailableCopies);

        var borrowingsQuery = _db.BorrowingRecords.AsQueryable();
        if (from.HasValue) borrowingsQuery = borrowingsQuery.Where(r => r.BorrowedAt >= from.Value.Date);
        if (to.HasValue) borrowingsQuery = borrowingsQuery.Where(r => r.BorrowedAt < to.Value.Date.AddDays(1));

        ViewBag.ActiveBorrowings = await borrowingsQuery.CountAsync(r => r.Status == "Active");
        ViewBag.OverdueCount = await borrowingsQuery.CountAsync(r => r.Status == "Active" && r.DueDate < now);
        ViewBag.TotalBorrowers = await _db.Borrowers.CountAsync(b => b.IsActive);

        var finesQuery = _db.Fines.AsQueryable();
        if (from.HasValue) finesQuery = finesQuery.Where(f => f.CalculatedAt >= from.Value.Date);
        if (to.HasValue) finesQuery = finesQuery.Where(f => f.CalculatedAt < to.Value.Date.AddDays(1));

        ViewBag.UnpaidFines = await finesQuery.CountAsync(f => f.Status == "Unpaid");
        ViewBag.TotalFinesCollected = await finesQuery.Where(f => f.Status == "Paid").SumAsync(f => f.Amount);

        var topBooks = await borrowingsQuery
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .Join(_db.Books, x => x.BookId, b => b.Id, (x, b) => new { b.Title, x.Count })
            .ToListAsync();

        ViewBag.TopBookLabels = System.Text.Json.JsonSerializer.Serialize(topBooks.Select(x => x.Title));
        ViewBag.TopBookData = System.Text.Json.JsonSerializer.Serialize(topBooks.Select(x => x.Count));

        var activityQuery = _db.AuditLogs.AsQueryable();
        if (from.HasValue) activityQuery = activityQuery.Where(a => a.Timestamp >= from.Value.Date);
        if (to.HasValue) activityQuery = activityQuery.Where(a => a.Timestamp < to.Value.Date.AddDays(1));

        var recentActivity = await activityQuery
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .Select(a => new
            {
                a.Action,
                a.EntityType,
                a.Details,
                a.Timestamp
            })
            .ToListAsync();

        ViewBag.RecentActivity = recentActivity;

        return View();
    }
}
