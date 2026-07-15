using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.Admin.Controllers;

// Read-only oversight of the catalog. Book records are owned by Librarian, which
// receives/shelves the physical copies and has the corresponding full CRUD UI.
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BooksController : Controller
{
    private readonly BookCatalogService _catalog;

    public BooksController(BookCatalogService catalog) => _catalog = catalog;

    public async Task<IActionResult> Index(string search, int page = 1)
    {
        var (items, totalPages) = await _catalog.GetPagedAsync(search, page);

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        return View(items);
    }
}
