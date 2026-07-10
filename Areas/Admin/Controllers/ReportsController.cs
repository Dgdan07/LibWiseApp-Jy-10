using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;

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

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? status, int page = 1)
    {
        var now = DateTime.UtcNow;

        ViewBag.TotalBooks = await _db.Books.CountAsync();
        ViewBag.TotalBorrowers = await _db.Borrowers.CountAsync();
        ViewBag.ActiveBorrowings = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active");
        ViewBag.OverdueCount = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active" && r.DueDate < now);
        ViewBag.UnpaidFinesCount = await _db.Fines.CountAsync(f => f.Status == "Unpaid");

        var borrowingsQuery = _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .AsQueryable();

        if (from.HasValue)
            borrowingsQuery = borrowingsQuery.Where(r => r.BorrowedAt >= from.Value.Date);
        if (to.HasValue)
            borrowingsQuery = borrowingsQuery.Where(r => r.BorrowedAt < to.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status == "Overdue")
                borrowingsQuery = borrowingsQuery.Where(r => r.Status == "Active" && r.DueDate < now);
            else
                borrowingsQuery = borrowingsQuery.Where(r => r.Status == status);
        }

        var total = await borrowingsQuery.CountAsync();
        var items = await borrowingsQuery
            .OrderByDescending(r => r.BorrowedAt)
            .Skip((page - 1) * 20)
            .Take(20)
            .ToListAsync();

        ViewBag.RecentBorrowings = items;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.Status = status;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetUnpaidFines()
    {
        var fines = await _db.Fines
            .Where(f => f.Status == "Unpaid")
            .Include(f => f.BorrowingRecord)
                .ThenInclude(r => r.Book)
            .Include(f => f.BorrowingRecord)
                .ThenInclude(r => r.Borrower)
            .OrderByDescending(f => f.CalculatedAt)
            .ToListAsync();

        var data = fines.Select(f => new
        {
            borrower = $"{f.BorrowingRecord.Borrower.LastName}, {f.BorrowingRecord.Borrower.FirstName}",
            book = f.BorrowingRecord.Book.Title,
            amount = f.Amount.ToString("F2"),
            dueDate = f.BorrowingRecord.DueDate.ToString("MMM dd, yyyy"),
            calculatedAt = f.CalculatedAt.ToString("MMM dd, yyyy")
        });

        return Json(data);
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
