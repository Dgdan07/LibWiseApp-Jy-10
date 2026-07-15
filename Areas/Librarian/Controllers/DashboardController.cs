using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.Librarian.Controllers;

[Area("Librarian")]
[Authorize(Roles = "Admin,Librarian")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly DashboardStatsService _stats;

    public DashboardController(AppDbContext db, DashboardStatsService stats)
    {
        _db = db;
        _stats = stats;
    }

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

        var defaultStart = now.Date.AddDays(-6);
        var defaultEnd = now.Date.AddDays(1);
        var (labels, borrowedData, returnedData) = await _stats.GetActivityAsync(defaultStart, defaultEnd);
        ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
        ViewBag.ChartBorrowed = System.Text.Json.JsonSerializer.Serialize(borrowedData);
        ViewBag.ChartReturned = System.Text.Json.JsonSerializer.Serialize(returnedData);

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ChartData(DateTime from, DateTime to)
    {
        var start = DashboardStatsService.ToUtcDate(from);
        var end = DashboardStatsService.ToUtcDate(to).AddDays(1);

        var (labels, borrowedData, returnedData) = await _stats.GetActivityAsync(start, end);

        return Json(new { labels, borrowed = borrowedData, returned = returnedData });
    }
}
