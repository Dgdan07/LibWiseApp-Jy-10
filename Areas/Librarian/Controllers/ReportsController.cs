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

        ViewBag.ActiveBorrowings = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active");
        ViewBag.OverdueCount = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active" && r.DueDate < now);
        ViewBag.UnpaidFinesCount = await _db.Fines.CountAsync(f => f.Status == "Unpaid");

        var overdueQuery = _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .Where(r => r.Status == "Active" && r.DueDate < now)
            .OrderBy(r => r.DueDate);

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

        var finesByBorrowerQuery = _db.Fines
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Borrower)
            .GroupBy(f => new { f.BorrowingRecord.BorrowerId, Name = f.BorrowingRecord.Borrower.LastName + ", " + f.BorrowingRecord.Borrower.FirstName, Barcode = f.BorrowingRecord.Borrower.Barcode })
            .Select(g => new
            {
                BorrowerName = g.Key.Name,
                Barcode = g.Key.Barcode,
                TotalUnpaid = g.Where(f => f.Status == "Unpaid").Sum(f => f.Amount),
                TotalPaid = g.Where(f => f.Status == "Paid").Sum(f => f.Amount),
                FineCount = g.Count()
            })
            .OrderByDescending(x => x.TotalUnpaid);

        var finesByBorrowerTotal = await finesByBorrowerQuery.CountAsync();
        var finesByBorrowerItems = await finesByBorrowerQuery
            .Skip((fbp - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        ViewBag.FinesByBorrower = finesByBorrowerItems;
        ViewBag.FinesByBorrowerPage = fbp;
        ViewBag.FinesByBorrowerTotalPages = (int)Math.Ceiling(finesByBorrowerTotal / (double)PageSize);

        var repeatOverdueQuery = _db.BorrowingRecords
            .Include(r => r.Borrower)
            .Where(r => (r.ReturnedAt.HasValue && r.DueDate < r.ReturnedAt) || (r.Status == "Active" && r.DueDate < now))
            .GroupBy(r => new { r.BorrowerId, Name = r.Borrower.LastName + ", " + r.Borrower.FirstName, Barcode = r.Borrower.Barcode })
            .Select(g => new
            {
                BorrowerName = g.Key.Name,
                Barcode = g.Key.Barcode,
                TotalOverdue = g.Count(),
                CurrentlyOverdue = g.Any(r => r.Status == "Active" && r.DueDate < now)
            })
            .OrderByDescending(x => x.TotalOverdue);

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
