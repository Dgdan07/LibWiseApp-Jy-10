using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;
using LibWiseApp.Services;

namespace LibWiseApp.Areas.Librarian.Controllers;

[Area("Librarian")]
[Authorize(Roles = "Admin,Librarian")]
public class BorrowersController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;

    public BorrowersController(AppDbContext db, AuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    private const int PageSize = 15;

    public async Task<IActionResult> Index(string search, int page = 1)
    {
        var borrowers = _db.Borrowers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            borrowers = borrowers.Where(b =>
                b.Barcode.Contains(search) ||
                b.FirstName.Contains(search) ||
                b.LastName.Contains(search) ||
                b.IDNumber!.Contains(search));

        var total = await borrowers.CountAsync();
        var items = await borrowers.OrderBy(b => b.LastName).ThenBy(b => b.FirstName)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetBorrower(int id)
    {
        var borrower = await _db.Borrowers.FindAsync(id);
        if (borrower == null) return NotFound();

        return Json(new
        {
            id = borrower.Id,
            barcode = borrower.Barcode,
            firstName = borrower.FirstName,
            lastName = borrower.LastName,
            email = borrower.Email,
            phone = borrower.Phone,
            grade = borrower.Grade,
            idNumber = borrower.IDNumber,
            address = borrower.Address,
            isActive = borrower.IsActive,
            createdAt = borrower.CreatedAt.ToString("o")
        });
    }

    private async Task<string> GenerateBarcodeAsync()
    {
        var lastBarcode = await _db.Borrowers
            .Where(b => b.Barcode.StartsWith("BRW-"))
            .OrderByDescending(b => b.Barcode)
            .Select(b => b.Barcode)
            .FirstOrDefaultAsync();

        if (lastBarcode != null && int.TryParse(lastBarcode.Replace("BRW-", ""), out int lastNum))
            return $"BRW-{(lastNum + 1).ToString("D4")}";

        return "BRW-0001";
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] Borrower model)
    {
        if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            return Json(new { success = false, error = "First Name and Last Name are required." });

        model.Barcode = await GenerateBarcodeAsync();
        model.CreatedAt = DateTime.UtcNow;
        _db.Borrowers.Add(model);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Create", "Borrower", model.Id.ToString(), $"Added borrower \"{model.FirstName} {model.LastName}\" (Barcode: {model.Barcode})");
        return Json(new { success = true, message = $"Borrower \"{model.FirstName} {model.LastName}\" added. Barcode: {model.Barcode}" });
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromForm] Borrower model)
    {
        var borrower = await _db.Borrowers.FindAsync(model.Id);
        if (borrower == null)
            return Json(new { success = false, error = "Borrower not found." });

        borrower.FirstName = model.FirstName;
        borrower.LastName = model.LastName;
        borrower.Email = model.Email;
        borrower.Phone = model.Phone;
        borrower.Address = model.Address;
        borrower.Grade = model.Grade;
        borrower.IDNumber = model.IDNumber;
        borrower.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Update", "Borrower", borrower.Id.ToString(), $"Updated borrower \"{borrower.FirstName} {borrower.LastName}\"");
        return Json(new { success = true, message = "Borrower updated." });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var borrower = await _db.Borrowers.FindAsync(id);
        if (borrower == null)
            return Json(new { success = false, message = "Borrower not found." });

        _db.Borrowers.Remove(borrower);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Delete", "Borrower", id.ToString(), $"Deleted borrower \"{borrower.FirstName} {borrower.LastName}\"");
        return Json(new { success = true, message = "Borrower deleted." });
    }
}
