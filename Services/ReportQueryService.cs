using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;

namespace LibWiseApp.Services;

public class ReportQueryService
{
    private readonly AppDbContext _db;

    public ReportQueryService(AppDbContext db) => _db = db;

    public IQueryable<BorrowingRecord> ActiveBorrowings() =>
        _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .Where(r => r.Status == "Active");

    public IQueryable<BorrowingRecord> OverdueBorrowings(DateTime now) =>
        ActiveBorrowings().Where(r => r.DueDate < now).OrderBy(r => r.DueDate);

    public IQueryable<FineByBorrowerRow> FinesByBorrower() =>
        _db.Fines
            .Include(f => f.BorrowingRecord).ThenInclude(r => r.Borrower)
            .GroupBy(f => new
            {
                f.BorrowingRecord.BorrowerId,
                Name = f.BorrowingRecord.Borrower.LastName + ", " + f.BorrowingRecord.Borrower.FirstName,
                Barcode = f.BorrowingRecord.Borrower.Barcode
            })
            .Select(g => new FineByBorrowerRow
            {
                BorrowerName = g.Key.Name,
                Barcode = g.Key.Barcode,
                TotalUnpaid = g.Where(f => f.Status == "Unpaid").Sum(f => f.Amount),
                TotalPaid = g.Where(f => f.Status == "Paid").Sum(f => f.Amount),
                FineCount = g.Count()
            })
            .OrderByDescending(x => x.TotalUnpaid);

    public IQueryable<RepeatOverdueRow> RepeatOverdueBorrowers(DateTime now) =>
        _db.BorrowingRecords
            .Include(r => r.Borrower)
            .Where(r => (r.ReturnedAt.HasValue && r.DueDate < r.ReturnedAt) || (r.Status == "Active" && r.DueDate < now))
            .GroupBy(r => new
            {
                r.BorrowerId,
                Name = r.Borrower.LastName + ", " + r.Borrower.FirstName,
                Barcode = r.Borrower.Barcode
            })
            .Select(g => new RepeatOverdueRow
            {
                BorrowerName = g.Key.Name,
                Barcode = g.Key.Barcode,
                TotalOverdue = g.Count(),
                CurrentlyOverdue = g.Any(r => r.Status == "Active" && r.DueDate < now)
            })
            .OrderByDescending(x => x.TotalOverdue);
}

public class FineByBorrowerRow
{
    public string BorrowerName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal TotalUnpaid { get; set; }
    public decimal TotalPaid { get; set; }
    public int FineCount { get; set; }
}

public class RepeatOverdueRow
{
    public string BorrowerName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int TotalOverdue { get; set; }
    public bool CurrentlyOverdue { get; set; }
}
