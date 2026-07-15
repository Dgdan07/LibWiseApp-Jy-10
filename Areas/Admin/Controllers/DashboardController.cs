using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly DashboardStatsService _stats;

    public DashboardController(AppDbContext db, DashboardStatsService stats)
    {
        _db = db;
        _stats = stats;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalBooks = await _db.Books.CountAsync();
        ViewBag.ActiveBorrowings = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active");
        ViewBag.OverdueCount = await _db.BorrowingRecords
            .CountAsync(r => r.Status == "Active" && r.DueDate < DateTime.UtcNow);
        ViewBag.UnpaidFinesCount = await _db.Fines.CountAsync(f => f.Status == "Unpaid");

        var now = DateTime.UtcNow;
        var defaultStart = now.Date.AddDays(-6);
        var defaultEnd = now.Date.AddDays(1);

        var (labels, borrowedData, returnedData) = await _stats.GetActivityAsync(defaultStart, defaultEnd);
        ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
        ViewBag.ChartBorrowed = System.Text.Json.JsonSerializer.Serialize(borrowedData);
        ViewBag.ChartReturned = System.Text.Json.JsonSerializer.Serialize(returnedData);

        var overdueRaw = await _db.BorrowingRecords
            .Where(r => r.Status == "Active" && r.DueDate < now)
            .OrderBy(r => r.DueDate)
            .Take(8)
            .Select(r => new
            {
                BorrowerName = r.Borrower.FirstName + " " + r.Borrower.LastName,
                r.Borrower.Barcode,
                BookTitle = r.Book.Title,
                r.DueDate
            })
            .ToListAsync();
        ViewBag.OverdueItems = overdueRaw
            .Select(x => new { x.BorrowerName, x.Barcode, x.BookTitle, x.DueDate, DaysOverdue = (now.Date - x.DueDate.Date).Days })
            .ToList();

        ViewBag.TopBooks = await _stats.GetTopBooksAsync(defaultStart, defaultEnd);
        ViewBag.TopCategories = await _stats.GetTopCategoriesAsync(defaultStart, defaultEnd);

        ViewBag.RecentActivity = await _db.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(8)
            .Select(a => new { a.Action, a.EntityType, a.Details, a.Timestamp })
            .ToListAsync();

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ChartData(DateTime from, DateTime to)
    {
        var start = DashboardStatsService.ToUtcDate(from);
        var end = DashboardStatsService.ToUtcDate(to).AddDays(1);

        var (labels, borrowedData, returnedData) = await _stats.GetActivityAsync(start, end);
        var topBooks = await _stats.GetTopBooksAsync(start, end);
        var topCategories = await _stats.GetTopCategoriesAsync(start, end);

        return Json(new { labels, borrowed = borrowedData, returned = returnedData, topBooks, topCategories });
    }
}
