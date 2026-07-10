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
    private readonly BorrowingService _borrowingService;

    public BorrowingController(AppDbContext db, UserManager<ApplicationUser> userManager, AuditLogService auditLog, BorrowingService borrowingService)
    {
        _db = db;
        _userManager = userManager;
        _auditLog = auditLog;
        _borrowingService = borrowingService;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Process(string borrowerBarcode, int bookId)
    {
        var (success, message, record) = await _borrowingService.BorrowBookAsync(
            borrowerBarcode, bookId, _userManager.GetUserId(User)!);

        if (!success)
            return Json(new { success = false, message });

        return Json(new
        {
            success = true,
            message,
            borrowerName = $"{record!.Borrower.FirstName} {record.Borrower.LastName}",
            bookTitle = record.Book.Title,
            dueDate = record.DueDate.ToString("MMM dd, yyyy")
        });
    }

    [HttpPost]
    public async Task<IActionResult> Extend(int recordId)
    {
        var (success, message) = await _borrowingService.ExtendBorrowingAsync(
            recordId, _userManager.GetUserId(User)!);

        return Json(new { success, message });
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

    [HttpGet]
    public async Task<IActionResult> GetAvailableBooks()
    {
        var books = await _db.Books
            .Where(b => b.IsActive && b.AvailableCopies > 0)
            .OrderBy(b => b.Title)
            .Select(b => new { b.Id, b.Title, b.Author, b.ISBN, b.AvailableCopies })
            .ToListAsync();
        return Json(books);
    }
}
