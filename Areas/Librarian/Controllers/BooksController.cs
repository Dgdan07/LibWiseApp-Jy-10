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
    private readonly BookCoverService _coverService;
    private const int PageSize = 10;

    public BooksController(AppDbContext db, AuditLogService auditLog, BookCoverService coverService)
    {
        _db = db;
        _auditLog = auditLog;
        _coverService = coverService;
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
    public async Task<IActionResult> Create([FromForm] Book model, IFormFile? coverImage)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Author))
            return Json(new { success = false, error = "Title and Author are required." });

        var coverError = await TryApplyCoverAsync(model, coverImage);
        if (coverError != null)
            return Json(new { success = false, error = coverError });

        model.CreatedAt = DateTime.UtcNow;
        _db.Books.Add(model);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Create", "Book", model.Id.ToString(), $"Added book \"{model.Title}\"");
        return Json(new { success = true, message = $"Book \"{model.Title}\" added." });
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromForm] Book model, IFormFile? coverImage)
    {
        var book = await _db.Books.FindAsync(model.Id);
        if (book == null)
            return Json(new { success = false, error = "Book not found." });

        var coverError = await TryApplyCoverAsync(book, coverImage);
        if (coverError != null)
            return Json(new { success = false, error = coverError });

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

    private async Task<string?> TryApplyCoverAsync(Book book, IFormFile? coverImage)
    {
        if (coverImage == null || coverImage.Length == 0)
            return null;

        if (coverImage.Length > BookCoverService.MaxUploadBytes)
            return "Cover image must be 15 MB or smaller.";

        try
        {
            using var stream = coverImage.OpenReadStream();
            var (bytes, contentType) = await _coverService.ProcessAsync(stream);
            book.CoverImage = bytes;
            book.CoverImageContentType = contentType;
            return null;
        }
        catch (SixLabors.ImageSharp.UnknownImageFormatException)
        {
            return "Cover image file isn't a recognized image format.";
        }
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
