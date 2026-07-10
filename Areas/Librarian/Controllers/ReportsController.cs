using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.Librarian.Controllers;

[Area("Librarian")]
[Authorize(Roles = "Admin,Librarian")]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> ExportCsv()
    {
        var now = DateTime.UtcNow;
        var records = await _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .Include(r => r.BorrowedBy)
            .Include(r => r.Fines)
            .OrderByDescending(r => r.BorrowedAt)
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Book Title,Author,Borrower,Barcode,Borrowed By,Borrowed At,Due Date,Returned At,Status,Fine Amount,Fine Status");
        foreach (var r in records)
        {
            var fine = r.Fines.FirstOrDefault();
            var returned = r.ReturnedAt.HasValue ? r.ReturnedAt.Value.ToString("yyyy-MM-dd") : "";
            sb.AppendLine($"\"{r.Book.Title}\",\"{r.Book.Author}\",\"{r.Borrower.LastName}, {r.Borrower.FirstName}\",{r.BorrowerBarcode},{r.BorrowedBy?.UserName},{r.BorrowedAt:yyyy-MM-dd},{r.DueDate:yyyy-MM-dd},{returned},{r.Status},{fine?.Amount},{fine?.Status}");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"libwise-operations-{now:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        ViewBag.ActiveBorrowings = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active");
        ViewBag.OverdueCount = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active" && r.DueDate < now);
        ViewBag.TotalFinesCollected = await _db.Fines.Where(f => f.Status == "Paid").SumAsync(f => f.Amount);
        ViewBag.UnpaidFinesCount = await _db.Fines.CountAsync(f => f.Status == "Unpaid");

        var recentReturns = await _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .Where(r => r.ReturnedAt >= now.AddDays(-7))
            .OrderByDescending(r => r.ReturnedAt)
            .Take(10)
            .ToListAsync();
        ViewBag.RecentReturns = recentReturns;

        var overdueBooks = await _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .Where(r => r.Status == "Active" && r.DueDate < now)
            .OrderBy(r => r.DueDate)
            .Take(20)
            .ToListAsync();
        ViewBag.OverdueBooks = overdueBooks;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveBorrowings()
    {
        var records = await _db.BorrowingRecords
            .Where(r => r.Status == "Active")
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .OrderByDescending(r => r.BorrowedAt)
            .ToListAsync();

        var data = records.Select(r => new
        {
            borrower = $"{r.Borrower.LastName}, {r.Borrower.FirstName}",
            book = r.Book.Title,
            borrowedAt = r.BorrowedAt.ToString("MMM dd, yyyy"),
            dueDate = r.DueDate.ToString("MMM dd, yyyy")
        });

        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> GetOverdueBorrowings()
    {
        var records = await _db.BorrowingRecords
            .Where(r => r.Status == "Active" && r.DueDate < DateTime.UtcNow)
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .OrderBy(r => r.DueDate)
            .ToListAsync();

        var data = records.Select(r => new
        {
            borrower = $"{r.Borrower.LastName}, {r.Borrower.FirstName}",
            book = r.Book.Title,
            borrowedAt = r.BorrowedAt.ToString("MMM dd, yyyy"),
            dueDate = r.DueDate.ToString("MMM dd, yyyy"),
            daysOverdue = (DateTime.UtcNow - r.DueDate).Days
        });

        return Json(data);
    }
}
