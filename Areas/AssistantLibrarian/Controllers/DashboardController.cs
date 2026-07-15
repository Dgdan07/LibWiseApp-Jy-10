using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.AssistantLibrarian.Controllers;

[Area("AssistantLibrarian")]
[Authorize(Roles = "Admin,Librarian,AssistantLibrarian")]
public class DashboardController : Controller
{
    private static readonly string[] RecentActivityActions = { "Borrow", "Return", "PayFine" };

    private readonly AppDbContext _db;
    private readonly DashboardStatsService _stats;

    public DashboardController(AppDbContext db, DashboardStatsService stats)
    {
        _db = db;
        _stats = stats;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var userId = _db.Users.Where(u => u.UserName == User.Identity!.Name).Select(u => u.Id).FirstOrDefault();

        ViewBag.TodayBorrowings = await _db.BorrowingRecords
            .CountAsync(r => r.BorrowedByUserId == userId && r.BorrowedAt >= today);
        ViewBag.TodayReturns = await _db.BorrowingRecords
            .CountAsync(r => r.ReturnedByUserId == userId && r.ReturnedAt >= today);
        ViewBag.TodayFinesCollected = await _db.Fines
            .Where(f => f.PaidByUserId == userId && f.PaidAt >= today && f.Status == "Paid")
            .SumAsync(f => f.Amount);

        ViewBag.OverdueCount = await _db.BorrowingRecords
            .CountAsync(r => r.Status == "Active" && r.DueDate < today);
        ViewBag.DueTodayCount = await _db.BorrowingRecords
            .CountAsync(r => r.Status == "Active" && r.DueDate >= today && r.DueDate < tomorrow);
        ViewBag.UnpaidFinesCount = await _db.Fines.CountAsync(f => f.Status == "Unpaid");

        var overdueRaw = await _db.BorrowingRecords
            .Where(r => r.Status == "Active" && r.DueDate < today)
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
            .Select(x => new { x.BorrowerName, x.Barcode, x.BookTitle, x.DueDate, DaysOverdue = (today - x.DueDate.Date).Days })
            .ToList();

        ViewBag.RecentActivity = await _db.AuditLogs
            .Where(a => RecentActivityActions.Contains(a.Action))
            .OrderByDescending(a => a.Timestamp)
            .Take(8)
            .Select(a => new { a.Action, a.EntityType, a.Details, a.Timestamp })
            .ToListAsync();

        var defaultStart = today.AddDays(-6);
        var defaultEnd = tomorrow;
        var (labels, borrowedData, returnedData) = await _stats.GetActivityAsync(defaultStart, defaultEnd);
        ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
        ViewBag.ChartBorrowed = System.Text.Json.JsonSerializer.Serialize(borrowedData);
        ViewBag.ChartReturned = System.Text.Json.JsonSerializer.Serialize(returnedData);

        ViewBag.TopBooks = await _stats.GetTopBooksAsync(defaultStart, defaultEnd);
        ViewBag.TopCategories = await _stats.GetTopCategoriesAsync(defaultStart, defaultEnd);

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
