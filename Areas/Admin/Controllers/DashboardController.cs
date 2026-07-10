using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalBooks = await _db.Books.CountAsync();
        ViewBag.ActiveBorrowings = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active");
        ViewBag.OverdueCount = await _db.BorrowingRecords
            .CountAsync(r => r.Status == "Active" && r.DueDate < DateTime.UtcNow);
        ViewBag.UnpaidFinesCount = await _db.Fines.CountAsync(f => f.Status == "Unpaid");

        var now = DateTime.UtcNow;
        var labels = new List<string>();
        var borrowedData = new List<int>();
        var returnedData = new List<int>();
        for (int i = 6; i >= 0; i--)
        {
            var day = now.Date.AddDays(-i);
            labels.Add(day.ToString("MMM dd"));
            borrowedData.Add(await _db.BorrowingRecords.CountAsync(r => r.BorrowedAt >= day && r.BorrowedAt < day.AddDays(1)));
            returnedData.Add(await _db.BorrowingRecords.CountAsync(r => r.ReturnedAt >= day && r.ReturnedAt < day.AddDays(1)));
        }
        ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
        ViewBag.ChartBorrowed = System.Text.Json.JsonSerializer.Serialize(borrowedData);
        ViewBag.ChartReturned = System.Text.Json.JsonSerializer.Serialize(returnedData);

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ChartData(DateTime from, DateTime to)
    {
        to = to.Date.AddDays(1);
        var labels = new List<string>();
        var borrowedData = new List<int>();
        var returnedData = new List<int>();

        for (var day = from.Date; day < to; day = day.AddDays(1))
        {
            labels.Add(day.ToString("MMM dd"));
            borrowedData.Add(await _db.BorrowingRecords.CountAsync(r => r.BorrowedAt >= day && r.BorrowedAt < day.AddDays(1)));
            returnedData.Add(await _db.BorrowingRecords.CountAsync(r => r.ReturnedAt >= day && r.ReturnedAt < day.AddDays(1)));
        }

        return Json(new { labels, borrowed = borrowedData, returned = returnedData });
    }
}
