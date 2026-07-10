using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.Librarian.Controllers;

[Area("Librarian")]
[Authorize(Roles = "Admin,Librarian")]
public class BooksController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;
    private const int PageSize = 15;

    public BooksController(AppDbContext db, AuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<IActionResult> Index(string search, int page = 1)
    {
        var books = _db.Books.Include(b => b.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            books = books.Where(b =>
                b.Title.Contains(search) ||
                b.Author.Contains(search) ||
                b.ISBN!.Contains(search));

        var total = await books.CountAsync();
        var items = await books.OrderBy(b => b.Title)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetBook(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

        return Json(new
        {
            id = book.Id,
            isbn = book.ISBN,
            title = book.Title,
            author = book.Author,
            publisher = book.Publisher,
            categoryId = book.CategoryId,
            publicationYear = book.PublicationYear,
            totalCopies = book.TotalCopies,
            availableCopies = book.AvailableCopies,
            shelfLocation = book.ShelfLocation,
            description = book.Description,
            isActive = book.IsActive,
            createdAt = book.CreatedAt.ToString("o")
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] Book model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Author))
            return Json(new { success = false, error = "Title and Author are required." });

        model.CreatedAt = DateTime.UtcNow;
        _db.Books.Add(model);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Create", "Book", model.Id.ToString(), $"Added book \"{model.Title}\"");
        return Json(new { success = true, message = $"Book \"{model.Title}\" added." });
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromForm] Book model)
    {
        var book = await _db.Books.FindAsync(model.Id);
        if (book == null)
            return Json(new { success = false, error = "Book not found." });

        book.ISBN = model.ISBN;
        book.Title = model.Title;
        book.Author = model.Author;
        book.Publisher = model.Publisher;
        book.CategoryId = model.CategoryId;
        book.PublicationYear = model.PublicationYear;
        book.TotalCopies = model.TotalCopies;
        book.AvailableCopies = model.AvailableCopies;
        book.ShelfLocation = model.ShelfLocation;
        book.Description = model.Description;
        book.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Update", "Book", book.Id.ToString(), $"Updated book \"{book.Title}\"");
        return Json(new { success = true, message = $"Book \"{book.Title}\" updated." });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null)
            return Json(new { success = false, message = "Book not found." });

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Delete", "Book", id.ToString(), $"Deleted book \"{book.Title}\"");
        return Json(new { success = true, message = $"Book \"{book.Title}\" deleted." });
    }
}
