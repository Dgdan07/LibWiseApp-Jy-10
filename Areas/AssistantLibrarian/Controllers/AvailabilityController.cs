using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.AssistantLibrarian.Controllers;

[Area("AssistantLibrarian")]
[Authorize(Roles = "Admin,Librarian,AssistantLibrarian")]
public class AvailabilityController : Controller
{
    private readonly AppDbContext _db;

    public AvailabilityController(AppDbContext db) => _db = db;

    private const int PageSize = 12;

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
}
