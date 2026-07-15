using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Services;

public class DashboardStatsService
{
    private readonly AppDbContext _db;

    public DashboardStatsService(AppDbContext db) => _db = db;

    public static DateTime ToUtcDate(DateTime dt) => DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);

    public async Task<(List<string> Labels, List<int> Borrowed, List<int> Returned)> GetActivityAsync(DateTime start, DateTime end)
    {
        var labels = new List<string>();
        var borrowed = new List<int>();
        var returned = new List<int>();

        for (var day = start; day < end; day = day.AddDays(1))
        {
            labels.Add(day.ToString("MMM dd"));
            borrowed.Add(await _db.BorrowingRecords.CountAsync(r => r.BorrowedAt >= day && r.BorrowedAt < day.AddDays(1)));
            returned.Add(await _db.BorrowingRecords.CountAsync(r => r.ReturnedAt >= day && r.ReturnedAt < day.AddDays(1)));
        }

        return (labels, borrowed, returned);
    }

    public async Task<List<TopBookDto>> GetTopBooksAsync(DateTime start, DateTime end, int take = 5)
    {
        return await _db.BorrowingRecords
            .Where(r => r.BorrowedAt >= start && r.BorrowedAt < end)
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(take)
            .Join(_db.Books, x => x.BookId, b => b.Id, (x, b) => new TopBookDto { Title = b.Title, Author = b.Author, Count = x.Count })
            .ToListAsync();
    }

    public async Task<List<TopCategoryDto>> GetTopCategoriesAsync(DateTime start, DateTime end, int take = 5)
    {
        return await _db.BorrowingRecords
            .Where(r => r.BorrowedAt >= start && r.BorrowedAt < end && r.Book.CategoryId != null)
            .GroupBy(r => r.Book.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(take)
            .Join(_db.Categories, x => x.CategoryId, c => c.Id, (x, c) => new TopCategoryDto { Name = c.Name, Count = x.Count })
            .ToListAsync();
    }
}

public class TopBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
