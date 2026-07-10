using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.Librarian.Controllers;

[Area("Librarian")]
[Authorize(Roles = "Admin,Librarian")]
public class FinesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLog;

    public FinesController(AppDbContext db, UserManager<ApplicationUser> userManager, AuditLogService auditLog)
    {
        _db = db;
        _userManager = userManager;
        _auditLog = auditLog;
    }

    private const int PageSize = 15;

    public async Task<IActionResult> Index(string status, int page = 1)
    {
        var fines = _db.Fines
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Book)
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Borrower)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            fines = fines.Where(f => f.Status == status);

        var total = await fines.CountAsync();
        var items = await fines
            .OrderByDescending(f => f.CalculatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> Pay(int id)
    {
        var fine = await _db.Fines
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Book)
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Borrower)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fine == null) return NotFound();

        fine.Status = "Paid";
        fine.PaidAt = DateTime.UtcNow;
        fine.PaidByUserId = _userManager.GetUserId(User);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("PayFine", "Fine", id.ToString(), $"Collected PHP {fine.Amount:F2}");

        TempData["Success"] = $"Payment of PHP {fine.Amount:F2} recorded.";
        TempData["Receipt"] = $"Borrower: {fine.BorrowingRecord.Borrower.FirstName} {fine.BorrowingRecord.Borrower.LastName} | Book: {fine.BorrowingRecord.Book.Title} | Amount: PHP {fine.Amount:F2} | Date: {fine.PaidAt:MMM dd, yyyy h:mm tt}";
        return RedirectToAction("Index", new { status = Request.Query["status"], page = Request.Query["page"] });
    }
}
