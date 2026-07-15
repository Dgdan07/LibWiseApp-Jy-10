using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.AssistantLibrarian.Controllers;

[Area("AssistantLibrarian")]
[Authorize(Roles = "Admin,Librarian,AssistantLibrarian")]
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

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Search(string borrowerBarcode)
    {
        var borrower = await _db.Borrowers.FirstOrDefaultAsync(b => b.Barcode == borrowerBarcode && b.IsActive);
        if (borrower == null)
            return Json(new { success = false, message = "Borrower not found." });

        var fines = await _db.Fines
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Book)
            .Where(f => f.BorrowingRecord.BorrowerId == borrower.Id && f.Status == "Unpaid")
            .Select(f => new { f.Id, BookTitle = f.BorrowingRecord.Book.Title, f.Amount, f.CalculatedAt })
            .ToListAsync();

        return Json(new { success = true, borrowerName = $"{borrower.FirstName} {borrower.LastName}", fines });
    }

    [HttpPost]
    public async Task<IActionResult> Pay(int fineId)
    {
        var fine = await _db.Fines
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Book)
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Borrower)
            .FirstOrDefaultAsync(f => f.Id == fineId);

        if (fine == null || fine.Status == "Paid")
            return Json(new { success = false, message = "Fine not found or already paid." });

        fine.Status = "Paid";
        fine.PaidAt = DateTime.UtcNow;
        fine.PaidByUserId = _userManager.GetUserId(User);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("PayFine", "Fine", fineId.ToString(), $"Collected PHP {fine.Amount:F2} (AL)");

        return Json(new
        {
            success = true,
            message = $"Payment of PHP {fine.Amount:F2} recorded.",
            receipt = new
            {
                borrower = $"{fine.BorrowingRecord.Borrower.FirstName} {fine.BorrowingRecord.Borrower.LastName}",
                book = fine.BorrowingRecord.Book.Title,
                amount = fine.Amount.ToString("F2"),
                paidAt = fine.PaidAt.Value.ToString("MMM dd, yyyy h:mm tt"),
                barcode = fine.BorrowingRecord.BorrowerBarcode
            }
        });
    }

    [HttpGet]
    public async Task<IActionResult> SearchBorrower(string term)
    {
        var borrowers = await _db.Borrowers
            .Where(b => b.IsActive && (b.Barcode.Contains(term) || b.FirstName.Contains(term) || b.LastName.Contains(term)))
            .Take(10)
            .Select(b => new { b.Id, b.Barcode, Name = b.FirstName + " " + b.LastName, b.Grade })
            .ToListAsync();
        return Json(borrowers);
    }
}
