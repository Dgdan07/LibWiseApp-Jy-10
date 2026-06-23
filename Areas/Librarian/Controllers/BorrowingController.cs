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
public class BorrowingController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLog;

    public BorrowingController(AppDbContext db, UserManager<ApplicationUser> userManager, AuditLogService auditLog)
    {
        _db = db;
        _userManager = userManager;
        _auditLog = auditLog;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Process(string borrowerBarcode, int bookId)
    {
        var borrower = await _db.Borrowers.FirstOrDefaultAsync(b => b.Barcode == borrowerBarcode && b.IsActive);
        if (borrower == null)
            return Json(new { success = false, message = "Borrower not found." });

        var book = await _db.Books.FindAsync(bookId);
        if (book == null)
            return Json(new { success = false, message = "Book not found." });

        if (book.AvailableCopies <= 0)
            return Json(new { success = false, message = "No available copies." });

        var fineRule = await _db.FineRules.FirstOrDefaultAsync(r => r.IsActive);
        var dueDate = DateTime.UtcNow.AddDays(fineRule?.DaysAllowed ?? 14);

        var record = new BorrowingRecord
        {
            BookId = book.Id,
            BorrowerId = borrower.Id,
            BorrowerBarcode = borrowerBarcode,
            BorrowedByUserId = _userManager.GetUserId(User)!,
            BorrowedAt = DateTime.UtcNow,
            DueDate = dueDate,
            Status = "Active"
        };

        book.AvailableCopies--;

        _db.BorrowingRecords.Add(record);
        _db.BookStatusLogs.Add(new BookStatusLog
        {
            BookId = book.Id,
            Status = "Borrowed",
            ChangedByUserId = _userManager.GetUserId(User),
            Remarks = $"Borrowed by {borrower.FirstName} {borrower.LastName} (barcode: {borrowerBarcode})"
        });

        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Borrow", "BorrowingRecord", "", $"Book \"{book.Title}\" (ID:{book.Id}) borrowed by {borrower.FirstName} {borrower.LastName} (barcode:{borrowerBarcode})");

        return Json(new
        {
            success = true,
            message = $"\"{book.Title}\" borrowed by {borrower.FirstName} {borrower.LastName}. Due: {dueDate:MMM dd, yyyy}",
            borrowerName = $"{borrower.FirstName} {borrower.LastName}",
            bookTitle = book.Title,
            dueDate = dueDate.ToString("MMM dd, yyyy")
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

    [HttpGet]
    public async Task<IActionResult> SearchBook(string term)
    {
        var books = await _db.Books
            .Where(b => b.IsActive && b.AvailableCopies > 0 &&
                (b.Title.Contains(term) || b.Author.Contains(term) || b.ISBN!.Contains(term)))
            .Take(10)
            .Select(b => new { b.Id, b.Title, b.Author, b.ISBN, b.AvailableCopies, Status = "Available" })
            .ToListAsync();
        return Json(books);
    }
}
