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

    public async Task<IActionResult> Index(string search)
    {
        var books = _db.Books.Include(b => b.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            books = books.Where(b =>
                b.Title.Contains(search) ||
                b.Author.Contains(search) ||
                b.ISBN!.Contains(search));

        ViewBag.Search = search;
        return View(await books.OrderBy(b => b.Title).ToListAsync());
    }
}
