using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;

namespace LibWiseApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ConfigController : Controller
{
    private readonly AppDbContext _db;

    public ConfigController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.FineRules = await _db.FineRules.OrderByDescending(r => r.IsActive).ToListAsync();
        ViewBag.SystemConfigs = await _db.SystemConfigs.OrderBy(c => c.Key).ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SaveFineRule(FineRule model)
    {
        var rule = await _db.FineRules.FindAsync(model.Id) ?? new FineRule();
        rule.DaysAllowed = model.DaysAllowed;
        rule.DailyFineRate = model.DailyFineRate;
        rule.MaxFine = model.MaxFine;
        rule.IsActive = model.IsActive;

        if (model.Id == 0) _db.FineRules.Add(rule);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Fine rule saved.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> SaveConfig(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            TempData["Error"] = "Key is required.";
            return RedirectToAction("Index");
        }

        var cfg = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key.Trim());
        if (cfg == null)
        {
            cfg = new SystemConfig { Key = key.Trim(), Value = value?.Trim() ?? "" };
            _db.SystemConfigs.Add(cfg);
        }
        else
        {
            cfg.Value = value?.Trim() ?? "";
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Configuration saved.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteConfig(int id)
    {
        var cfg = await _db.SystemConfigs.FindAsync(id);
        if (cfg != null)
        {
            _db.SystemConfigs.Remove(cfg);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Configuration deleted.";
        }
        return RedirectToAction("Index");
    }
}
