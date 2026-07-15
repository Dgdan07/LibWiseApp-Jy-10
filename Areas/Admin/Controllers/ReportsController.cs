using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ReportQueryService _reportQuery;

    public ReportsController(AppDbContext db, ReportQueryService reportQuery)
    {
        _db = db;
        _reportQuery = reportQuery;
    }

    private const int PageSize = 10;

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

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? status, int page = 1, int? finesByBorrowerPage = null, int? repeatOverduePage = null)
    {
        var now = DateTime.UtcNow;
        int fbp = finesByBorrowerPage ?? 1;
        int rop = repeatOverduePage ?? 1;

        ViewBag.TotalBooks = await _db.Books.CountAsync();
        ViewBag.TotalBorrowers = await _db.Borrowers.CountAsync();
        ViewBag.ActiveBorrowings = await _reportQuery.ActiveBorrowings().CountAsync();
        ViewBag.OverdueCount = await _reportQuery.OverdueBorrowings(now).CountAsync();
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
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.RecentBorrowings = items;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.Status = status;

        var finesByBorrowerQuery = _reportQuery.FinesByBorrower();
        var finesByBorrowerTotal = await finesByBorrowerQuery.CountAsync();
        var finesByBorrowerItems = await finesByBorrowerQuery
            .Skip((fbp - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        ViewBag.FinesByBorrower = finesByBorrowerItems;
        ViewBag.FinesByBorrowerPage = fbp;
        ViewBag.FinesByBorrowerTotalPages = (int)Math.Ceiling(finesByBorrowerTotal / (double)PageSize);

        var repeatOverdueQuery = _reportQuery.RepeatOverdueBorrowers(now);
        var repeatOverdueTotal = await repeatOverdueQuery.CountAsync();
        var repeatOverdueItems = await repeatOverdueQuery
            .Skip((rop - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        ViewBag.RepeatOverdueBorrowers = repeatOverdueItems;
        ViewBag.RepeatOverduePage = rop;
        ViewBag.RepeatOverdueTotalPages = (int)Math.Ceiling(repeatOverdueTotal / (double)PageSize);

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetUnpaidFines(int page = 1)
    {
        var query = _db.Fines
            .Where(f => f.Status == "Unpaid")
            .Include(f => f.BorrowingRecord)
                .ThenInclude(r => r.Book)
            .Include(f => f.BorrowingRecord)
                .ThenInclude(r => r.Borrower)
            .OrderByDescending(f => f.CalculatedAt);

        var total = await query.CountAsync();
        var fines = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var data = fines.Select(f => new
        {
            borrower = $"{f.BorrowingRecord.Borrower.LastName}, {f.BorrowingRecord.Borrower.FirstName}",
            book = f.BorrowingRecord.Book.Title,
            amount = f.Amount.ToString("F2"),
            dueDate = f.BorrowingRecord.DueDate.ToString("MMM dd, yyyy"),
            calculatedAt = f.CalculatedAt.ToString("MMM dd, yyyy")
        });

        return Json(new { items = data, page, totalPages = (int)Math.Ceiling(total / (double)PageSize) });
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveBorrowings(int page = 1)
    {
        var query = _reportQuery.ActiveBorrowings().OrderByDescending(r => r.BorrowedAt);

        var total = await query.CountAsync();
        var records = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var data = records.Select(r => new
        {
            borrower = $"{r.Borrower.LastName}, {r.Borrower.FirstName}",
            book = r.Book.Title,
            borrowedAt = r.BorrowedAt.ToString("MMM dd, yyyy"),
            dueDate = r.DueDate.ToString("MMM dd, yyyy")
        });

        return Json(new { items = data, page, totalPages = (int)Math.Ceiling(total / (double)PageSize) });
    }

    [HttpGet]
    public async Task<IActionResult> GetOverdueBorrowings(int page = 1)
    {
        var now = DateTime.UtcNow;
        var query = _reportQuery.OverdueBorrowings(now);

        var total = await query.CountAsync();
        var records = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var data = records.Select(r => new
        {
            borrower = $"{r.Borrower.LastName}, {r.Borrower.FirstName}",
            book = r.Book.Title,
            borrowedAt = r.BorrowedAt.ToString("MMM dd, yyyy"),
            dueDate = r.DueDate.ToString("MMM dd, yyyy"),
            daysOverdue = (now - r.DueDate).Days
        });

        return Json(new { items = data, page, totalPages = (int)Math.Ceiling(total / (double)PageSize) });
    }
}
