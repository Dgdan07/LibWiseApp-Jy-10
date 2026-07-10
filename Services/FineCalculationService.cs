using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;

namespace LibWiseApp.Services;

public class FineCalculationService
{
    private readonly AppDbContext _db;

    public FineCalculationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(Fine? Fine, string Message)> CalculateFineAsync(BorrowingRecord record)
    {
        if (record.ReturnedAt == null || record.ReturnedAt <= record.DueDate)
            return (null, "");

        var fineRule = await _db.FineRules.FirstOrDefaultAsync(r => r.IsActive);
        var daysOverdue = (int)(record.ReturnedAt.Value - record.DueDate).TotalDays;
        var amount = Math.Min(daysOverdue * (fineRule?.DailyFineRate ?? 5), fineRule?.MaxFine ?? 500);

        var fine = new Fine
        {
            BorrowingRecordId = record.Id,
            Amount = amount,
            CalculatedAt = DateTime.UtcNow,
            Status = "Unpaid"
        };

        _db.Fines.Add(fine);

        var message = $" Overdue by {daysOverdue} day(s). Fine: PHP {amount:F2}";
        return (fine, message);
    }
}
