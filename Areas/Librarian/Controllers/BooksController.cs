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
    private const int PageSize = 20;

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
        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Book model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        _db.Books.Add(model);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Create", "Book", model.Id.ToString(), $"Added book \"{model.Title}\"");
        TempData["Success"] = $"Book \"{model.Title}\" added.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(book);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Book model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

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
        TempData["Success"] = $"Book \"{book.Title}\" updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Delete", "Book", id.ToString(), $"Deleted book \"{book.Title}\"");
        TempData["Success"] = $"Book \"{book.Title}\" deleted.";
        return RedirectToAction("Index");
    }
}
