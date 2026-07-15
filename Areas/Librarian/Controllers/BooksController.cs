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
    private readonly BookCatalogService _catalog;

    public BooksController(AppDbContext db, AuditLogService auditLog, BookCatalogService catalog)
    {
        _db = db;
        _auditLog = auditLog;
        _catalog = catalog;
    }

    public async Task<IActionResult> Index(string search, int page = 1)
    {
        var (items, totalPages) = await _catalog.GetPagedAsync(search, page);

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetBook(int id)
    {
        var book = await _catalog.GetByIdAsync(id);
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
        var (success, error, book) = await _catalog.CreateAsync(model, coverImage);
        if (!success) return Json(new { success = false, error });

        await _auditLog.LogAsync("Create", "Book", book!.Id.ToString(), $"Added book \"{book.Title}\"");
        return Json(new { success = true, message = $"Book \"{book.Title}\" added." });
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromForm] Book model, IFormFile? coverImage)
    {
        var (success, error, book) = await _catalog.EditAsync(model, coverImage);
        if (!success) return Json(new { success = false, error });

        await _auditLog.LogAsync("Update", "Book", book!.Id.ToString(), $"Updated book \"{book.Title}\"");
        return Json(new { success = true, message = $"Book \"{book.Title}\" updated." });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _catalog.DeleteAsync(id);
        if (book == null)
            return Json(new { success = false, message = "Book not found." });

        await _auditLog.LogAsync("Delete", "Book", id.ToString(), $"Deleted book \"{book.Title}\"");
        return Json(new { success = true, message = $"Book \"{book.Title}\" deleted." });
    }
}
