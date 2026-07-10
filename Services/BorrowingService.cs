using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;

namespace LibWiseApp.Services;

public class BorrowingService
{
    private const int DefaultBorrowDays = 2;
    private const int ExtensionDays = 2;

    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;

    public BorrowingService(AppDbContext db, AuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<(bool Success, string Message, BorrowingRecord? Record)> BorrowBookAsync(
        string borrowerBarcode, int bookId, string userId)
    {
        var borrower = await _db.Borrowers.FirstOrDefaultAsync(b => b.Barcode == borrowerBarcode && b.IsActive);
        if (borrower == null)
            return (false, "Borrower not found.", null);

        var book = await _db.Books.FindAsync(bookId);
        if (book == null)
            return (false, "Book not found.", null);

        if (book.AvailableCopies <= 0)
            return (false, "No available copies.", null);

        var dueDate = DateTime.UtcNow.AddDays(DefaultBorrowDays);

        var record = new BorrowingRecord
        {
            BookId = book.Id,
            BorrowerId = borrower.Id,
            BorrowerBarcode = borrowerBarcode,
            BorrowedByUserId = userId,
            BorrowedAt = DateTime.UtcNow,
            DueDate = dueDate,
            Status = "Active"
        };

        book.AvailableCopies--;

        _db.BorrowingRecords.Add(record);
        _db.BookStatusLogs.Add(new BookStatusLog
        {
            BookId = book.Id,
            Status = "Borrowed",
            ChangedByUserId = userId,
            Remarks = $"Borrowed by {borrower.FirstName} {borrower.LastName} (barcode: {borrowerBarcode})"
        });

        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Borrow", "BorrowingRecord", record.Id.ToString(),
            $"Book \"{book.Title}\" (ID:{book.Id}) borrowed by {borrower.FirstName} {borrower.LastName}");

        return (true, $"\"{book.Title}\" borrowed. Due: {dueDate:MMM dd, yyyy}", record);
    }

    public async Task<(bool Success, string Message)> ExtendBorrowingAsync(int recordId, string userId)
    {
        var record = await _db.BorrowingRecords
            .Include(r => r.Book)
            .Include(r => r.Borrower)
            .FirstOrDefaultAsync(r => r.Id == recordId && r.Status == "Active");

        if (record == null)
            return (false, "Borrowing record not found.");

        if (record.WasExtended)
            return (false, "This borrowing has already been extended once.");

        var oneDayBeforeDue = record.DueDate.AddDays(-1);
        if (DateTime.UtcNow.Date != oneDayBeforeDue.Date)
            return (false, "Extension can only be done 1 day before the due date.");

        record.DueDate = record.DueDate.AddDays(ExtensionDays);
        record.WasExtended = true;

        await _db.SaveChangesAsync();
        await _auditLog.LogAsync("Extend", "BorrowingRecord", recordId.ToString(),
            $"Extended borrowing for \"{record.Book.Title}\" by {record.Borrower.FirstName} {record.Borrower.LastName}. New due: {record.DueDate:MMM dd, yyyy}");

        return (true, $"Borrowing extended. New due date: {record.DueDate:MMM dd, yyyy}");
    }
}
