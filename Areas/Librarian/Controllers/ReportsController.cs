using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.Librarian.Controllers;

[Area("Librarian")]
[Authorize(Roles = "Admin,Librarian")]
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
            var returned = r.ReturnedAt.HasValue ? r.ReturnedAt.Value.ToString("yyyy-MM-dd") : "";
            sb.AppendLine($"\"{r.Book.Title}\",\"{r.Book.Author}\",\"{r.Borrower.LastName}, {r.Borrower.FirstName}\",{r.BorrowerBarcode},{r.BorrowedBy?.UserName},{r.BorrowedAt:yyyy-MM-dd},{r.DueDate:yyyy-MM-dd},{returned},{r.Status},{fine?.Amount},{fine?.Status}");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"libwise-operations-{now:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> Index(int? overduePage, int? returnsPage, int? finesByBorrowerPage, int? repeatOverduePage)
    {
        var now = DateTime.UtcNow;
        int op = overduePage ?? 1;
        int rp = returnsPage ?? 1;
        int fbp = finesByBorrowerPage ?? 1;
        int rop = repeatOverduePage ?? 1;

        ViewBag.ActiveBorrowings = await _reportQuery.ActiveBorrowings().CountAsync();
        ViewBag.OverdueCount = await _reportQuery.OverdueBorrowings(now).CountAsync();
        ViewBag.UnpaidFinesCount = await _db.Fines.CountAsync(f => f.Status == "Unpaid");

        var overdueQuery = _reportQuery.OverdueBorrowings(now);
        var overdueTotal = await overdueQuery.CountAsync();
        var overdueBooks = await overdueQuery
            .Skip((op - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        ViewBag.OverdueBooks = overdueBooks;
        ViewBag.OverduePage = op;
        ViewBag.OverdueTotalPages = (int)Math.Ceiling(overdueTotal / (double)PageSize);

        var returnsQuery = _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .Where(r => r.ReturnedAt >= now.AddDays(-7))
            .OrderByDescending(r => r.ReturnedAt);

        var returnsTotal = await returnsQuery.CountAsync();
        var recentReturns = await returnsQuery
            .Skip((rp - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        ViewBag.RecentReturns = recentReturns;
        ViewBag.ReturnsPage = rp;
        ViewBag.ReturnsTotalPages = (int)Math.Ceiling(returnsTotal / (double)PageSize);

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
    public async Task<IActionResult> GetActiveBorrowings()
    {
        var records = await _reportQuery.ActiveBorrowings()
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
        var now = DateTime.UtcNow;
        var records = await _reportQuery.OverdueBorrowings(now).ToListAsync();

        var data = records.Select(r => new
        {
            borrower = $"{r.Borrower.LastName}, {r.Borrower.FirstName}",
            book = r.Book.Title,
            borrowedAt = r.BorrowedAt.ToString("MMM dd, yyyy"),
            dueDate = r.DueDate.ToString("MMM dd, yyyy"),
            daysOverdue = (now - r.DueDate).Days
        });

        return Json(data);
    }
}
