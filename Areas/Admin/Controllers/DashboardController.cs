using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalBooks = await _db.Books.CountAsync();
        ViewBag.ActiveBorrowings = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active");
        ViewBag.OverdueCount = await _db.BorrowingRecords
            .CountAsync(r => r.Status == "Active" && r.DueDate < DateTime.UtcNow);
        ViewBag.UnpaidFinesCount = await _db.Fines.CountAsync(f => f.Status == "Unpaid");

        var now = DateTime.UtcNow;
        var labels = new List<string>();
        var borrowedData = new List<int>();
        var returnedData = new List<int>();
        for (int i = 6; i >= 0; i--)
        {
            var day = now.Date.AddDays(-i);
            labels.Add(day.ToString("MMM dd"));
            borrowedData.Add(await _db.BorrowingRecords.CountAsync(r => r.BorrowedAt >= day && r.BorrowedAt < day.AddDays(1)));
            returnedData.Add(await _db.BorrowingRecords.CountAsync(r => r.ReturnedAt >= day && r.ReturnedAt < day.AddDays(1)));
        }
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

        ViewBag.TopBooks = await _db.BorrowingRecords
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .Join(_db.Books, x => x.BookId, b => b.Id, (x, b) => new { b.Title, b.Author, x.Count })
            .ToListAsync();

        ViewBag.TopCategories = await _db.BorrowingRecords
            .Where(r => r.Book.CategoryId != null)
            .GroupBy(r => r.Book.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .Join(_db.Categories, x => x.CategoryId, c => c.Id, (x, c) => new { c.Name, x.Count })
            .ToListAsync();

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
        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date, DateTimeKind.Utc).AddDays(1);
        var labels = new List<string>();
        var borrowedData = new List<int>();
        var returnedData = new List<int>();

        for (var day = start; day < end; day = day.AddDays(1))
        {
            labels.Add(day.ToString("MMM dd"));
            borrowedData.Add(await _db.BorrowingRecords.CountAsync(r => r.BorrowedAt >= day && r.BorrowedAt < day.AddDays(1)));
            returnedData.Add(await _db.BorrowingRecords.CountAsync(r => r.ReturnedAt >= day && r.ReturnedAt < day.AddDays(1)));
        }

        return Json(new { labels, borrowed = borrowedData, returned = returnedData });
    }
}
