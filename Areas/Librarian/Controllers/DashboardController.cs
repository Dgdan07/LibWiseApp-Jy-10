using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.Librarian.Controllers;

[Area("Librarian")]
[Authorize(Roles = "Admin,Librarian")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalBooks = await _db.Books.CountAsync();
        ViewBag.AvailableBooks = await _db.Books.SumAsync(b => b.AvailableCopies);
        ViewBag.ActiveBorrowings = await _db.BorrowingRecords.CountAsync(r => r.Status == "Active");
        ViewBag.UnpaidFines = await _db.Fines.CountAsync(f => f.Status == "Unpaid");
        ViewBag.TotalBorrowers = await _db.Borrowers.CountAsync(b => b.IsActive);
        ViewBag.TotalFinesCollected = await _db.Fines.Where(f => f.Status == "Paid").SumAsync(f => f.Amount);
        ViewBag.OverdueCount = await _db.BorrowingRecords
            .CountAsync(r => r.Status == "Active" && r.DueDate < DateTime.UtcNow);
        return View();
    }
}
