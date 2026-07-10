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
public class ReturnsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLog;
    private readonly FineCalculationService _fineCalc;
    private readonly BorrowingService _borrowingService;

    public ReturnsController(AppDbContext db, UserManager<ApplicationUser> userManager, AuditLogService auditLog, FineCalculationService fineCalc, BorrowingService borrowingService)
    {
        _db = db;
        _userManager = userManager;
        _auditLog = auditLog;
        _fineCalc = fineCalc;
        _borrowingService = borrowingService;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Process(string borrowerBarcode)
    {
        var borrower = await _db.Borrowers.FirstOrDefaultAsync(b => b.Barcode == borrowerBarcode && b.IsActive);
        if (borrower == null)
            return Json(new { success = false, message = "Borrower not found." });

        var activeRecords = await _db.BorrowingRecords
            .Include(r => r.Book)
            .Where(r => r.BorrowerId == borrower.Id && r.Status == "Active")
            .Select(r => new { r.Id, BookTitle = r.Book.Title, r.BookId, r.DueDate, r.BorrowedAt, r.WasExtended })
            .ToListAsync();

        if (activeRecords.Count == 0)
            return Json(new { success = false, message = "No active borrowings for this borrower." });

        return Json(new { success = true, borrowerName = $"{borrower.FirstName} {borrower.LastName}", books = activeRecords });
    }

    [HttpPost]
    public async Task<IActionResult> ReturnBook(int recordId)
    {
        var record = await _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .FirstOrDefaultAsync(r => r.Id == recordId && r.Status == "Active");

        if (record == null)
            return Json(new { success = false, message = "Record not found or already returned." });

        record.Status = "Returned";
        record.ReturnedAt = DateTime.UtcNow;
        record.ReturnedByUserId = _userManager.GetUserId(User);
        record.Book.AvailableCopies++;

        var (_, fineMessage) = await _fineCalc.CalculateFineAsync(record);

        _db.BookStatusLogs.Add(new BookStatusLog
        {
            BookId = record.BookId,
            Status = "Returned",
            ChangedByUserId = _userManager.GetUserId(User),
            Remarks = $"Returned by {record.Borrower.FirstName} {record.Borrower.LastName}"
        });

        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Return", "BorrowingRecord", recordId.ToString(), $"Book \"{record.Book.Title}\" returned{fineMessage}");

        return Json(new
        {
            success = true,
            message = $"\"{record.Book.Title}\" returned successfully.{fineMessage}",
            bookTitle = record.Book.Title,
            fine = fineMessage
        });
    }

    [HttpPost]
    public async Task<IActionResult> Extend(int recordId)
    {
        var (success, message) = await _borrowingService.ExtendBorrowingAsync(
            recordId, _userManager.GetUserId(User)!);

        return Json(new { success, message });
    }
}
