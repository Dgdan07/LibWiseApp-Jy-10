using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;
using LibWiseApp.Services;
using System.Threading.Tasks;

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

    private const int PageSize = 20;

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

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> CheckBarcode(string barcode, int? id)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return Json(true);

        var exists = id.HasValue
            ? await _db.Borrowers.AnyAsync(b => b.Barcode == barcode && b.Id != id.Value)
            : await _db.Borrowers.AnyAsync(b => b.Barcode == barcode);

        return Json(!exists);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(Borrower model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _db.Borrowers.AnyAsync(b => b.Barcode == model.Barcode))
        {
            ModelState.AddModelError("Barcode", "This barcode already exists.");
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        _db.Borrowers.Add(model);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Create", "Borrower", model.Id.ToString(), $"Added borrower \"{model.FirstName} {model.LastName}\"");
        TempData["Success"] = $"Borrower \"{model.FirstName} {model.LastName}\" added.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var borrower = await _db.Borrowers.FindAsync(id);
        if (borrower == null) return NotFound();
        return View(borrower);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Borrower model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var borrower = await _db.Borrowers.FindAsync(id);
        if (borrower == null) return NotFound();

        if (await _db.Borrowers.AnyAsync(b => b.Barcode == model.Barcode && b.Id != id))
        {
            ModelState.AddModelError("Barcode", "This barcode already exists.");
            return View(model);
        }

        borrower.Barcode = model.Barcode;
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
        TempData["Success"] = $"Borrower updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var borrower = await _db.Borrowers.FindAsync(id);
        if (borrower == null) return NotFound();

        _db.Borrowers.Remove(borrower);
        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Delete", "Borrower", id.ToString(), $"Deleted borrower \"{borrower.FirstName} {borrower.LastName}\"");
        TempData["Success"] = $"Borrower deleted.";
        return RedirectToAction("Index");
    }
}
