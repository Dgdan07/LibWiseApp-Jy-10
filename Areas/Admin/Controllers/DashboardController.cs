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
        ViewBag.AvailableCopies = await _db.Books.SumAsync(b => b.AvailableCopies);
        ViewBag.ActiveBorrowings = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active");
        ViewBag.OverdueCount = await _db.BorrowingRecords
            .CountAsync(r => r.Status == "Active" && r.DueDate < DateTime.UtcNow);
        ViewBag.TotalUsers = await _db.Users.CountAsync();
        ViewBag.TotalBorrowers = await _db.Borrowers.CountAsync();
        ViewBag.TotalFinesCollected = await _db.Fines.Where(f => f.Status == "Paid").SumAsync(f => f.Amount);
        ViewBag.UnpaidFinesTotal = await _db.Fines.Where(f => f.Status == "Unpaid").SumAsync(f => f.Amount);

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
}
