using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.AssistantLibrarian.Controllers;

[Area("AssistantLibrarian")]
[Authorize(Roles = "Admin,Librarian,AssistantLibrarian")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

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
            .OrderByDescending(a => a.Timestamp)
            .Take(8)
            .Select(a => new { a.Action, a.EntityType, a.Details, a.Timestamp })
            .ToListAsync();

        return View();
    }
}
