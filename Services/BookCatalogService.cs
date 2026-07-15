using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;

namespace LibWiseApp.Services;

public class BookCatalogService
{
    private readonly AppDbContext _db;
    private readonly BookCoverService _coverService;
    private const int PageSize = 10;

    public BookCatalogService(AppDbContext db, BookCoverService coverService)
    {
        _db = db;
        _coverService = coverService;
    }

    public async Task<(List<Book> Items, int TotalPages)> GetPagedAsync(string? search, int page)
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

        return (items, (int)Math.Ceiling(total / (double)PageSize));
    }

    public Task<Book?> GetByIdAsync(int id) => _db.Books.FindAsync(id).AsTask();

    public async Task<(bool Success, string? Error, Book? Book)> CreateAsync(Book model, IFormFile? coverImage)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Author))
            return (false, "Title and Author are required.", null);

        var coverError = await TryApplyCoverAsync(model, coverImage);
        if (coverError != null)
            return (false, coverError, null);

        model.CreatedAt = DateTime.UtcNow;
        _db.Books.Add(model);
        await _db.SaveChangesAsync();
        return (true, null, model);
    }

    public async Task<(bool Success, string? Error, Book? Book)> EditAsync(Book model, IFormFile? coverImage)
    {
        var book = await _db.Books.FindAsync(model.Id);
        if (book == null)
            return (false, "Book not found.", null);

        var coverError = await TryApplyCoverAsync(book, coverImage);
        if (coverError != null)
            return (false, coverError, null);

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
        return (true, null, book);
    }

    public async Task<Book?> DeleteAsync(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return null;

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        return book;
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
}
