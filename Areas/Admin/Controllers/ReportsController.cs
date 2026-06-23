using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
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
            sb.AppendLine($"\"{r.Book.Title}\",\"{r.Book.Author}\",\"{r.Borrower.LastName}, {r.Borrower.FirstName}\",{r.BorrowerBarcode},{r.BorrowedBy?.UserName},{r.BorrowedAt:yyyy-MM-dd},{r.DueDate:yyyy-MM-dd},{r.ReturnedAt:yyyy-MM-dd},{r.Status},{fine?.Amount},{fine?.Status}");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"libwise-report-{now:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        ViewBag.TotalBooks = await _db.Books.CountAsync();
        ViewBag.TotalBorrowers = await _db.Borrowers.CountAsync();
        ViewBag.ActiveBorrowings = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active");
        ViewBag.OverdueCount = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active" && r.DueDate < now);
        ViewBag.TotalFinesCollected = await _db.Fines.Where(f => f.Status == "Paid").SumAsync(f => f.Amount);
        ViewBag.UnpaidFinesCount = await _db.Fines.CountAsync(f => f.Status == "Unpaid");
        ViewBag.UnpaidFinesTotal = await _db.Fines.Where(f => f.Status == "Unpaid").SumAsync(f => f.Amount);

        var recentBorrowings = await _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .Where(r => r.BorrowedAt >= now.AddDays(-7))
            .OrderByDescending(r => r.BorrowedAt)
            .Take(10)
            .ToListAsync();
        ViewBag.RecentBorrowings = recentBorrowings;

        return View();
    }
}
