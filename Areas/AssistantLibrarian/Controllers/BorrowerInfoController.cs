using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;

namespace LibWiseApp.Areas.AssistantLibrarian.Controllers;

[Area("AssistantLibrarian")]
[Authorize(Roles = "Admin,Librarian,AssistantLibrarian")]
public class BorrowerInfoController : Controller
{
    private readonly AppDbContext _db;

    public BorrowerInfoController(AppDbContext db) => _db = db;

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Lookup(string barcode)
    {
        var borrower = await _db.Borrowers
            .Where(b => b.Barcode == barcode && b.IsActive)
            .Select(b => new
            {
                b.Barcode,
                b.FirstName,
                b.LastName,
                b.Email,
                b.Phone,
                b.Grade,
                b.IDNumber
            })
            .FirstOrDefaultAsync();

        if (borrower == null)
            return Json(new { success = false, message = "Borrower not found." });

        return Json(new { success = true, borrower });
    }
}
