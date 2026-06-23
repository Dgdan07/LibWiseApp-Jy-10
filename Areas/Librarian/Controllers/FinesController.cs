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

    public async Task<IActionResult> Index(string status)
    {
        var fines = _db.Fines
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Book)
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Borrower)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            fines = fines.Where(f => f.Status == status);

        ViewBag.Status = status;
        return View(await fines.OrderByDescending(f => f.CalculatedAt).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Pay(int id)
    {
        var fine = await _db.Fines.FindAsync(id);
        if (fine == null) return NotFound();

        fine.Status = "Paid";
        fine.PaidAt = DateTime.UtcNow;
        fine.PaidByUserId = _userManager.GetUserId(User);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("PayFine", "Fine", id.ToString(), $"Collected PHP {fine.Amount:F2}");

        TempData["Success"] = $"Payment of PHP {fine.Amount:F2} recorded.";
        return RedirectToAction("Index");
    }
}
