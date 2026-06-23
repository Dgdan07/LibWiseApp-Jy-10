using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.AssistantLibrarian.Controllers;

[Area("AssistantLibrarian")]
[Authorize(Roles = "Admin,Librarian,AssistantLibrarian")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var userId = _db.Users.Where(u => u.UserName == User.Identity!.Name).Select(u => u.Id).FirstOrDefault();

        ViewBag.TodayBorrowings = await _db.BorrowingRecords
            .CountAsync(r => r.BorrowedByUserId == userId && r.BorrowedAt >= today);
        ViewBag.TodayReturns = await _db.BorrowingRecords
            .CountAsync(r => r.ReturnedByUserId == userId && r.ReturnedAt >= today);
        ViewBag.TodayFinesCollected = await _db.Fines
            .Where(f => f.PaidByUserId == userId && f.PaidAt >= today && f.Status == "Paid")
            .SumAsync(f => f.Amount);
        return View();
    }
}
