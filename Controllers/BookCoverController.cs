using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Controllers;

[Authorize]
[Route("Books/[action]")]
public class BookCoverController : Controller
{
    private readonly AppDbContext _db;

    public BookCoverController(AppDbContext db) => _db = db;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Cover(int id)
    {
        var cover = await _db.Books
            .Where(b => b.Id == id)
            .Select(b => new { b.CoverImage, b.CoverImageContentType })
            .FirstOrDefaultAsync();

        if (cover?.CoverImage == null)
            return NotFound();

        Response.Headers.CacheControl = "private, max-age=3600";
        return File(cover.CoverImage, cover.CoverImageContentType ?? "image/jpeg");
    }
}
