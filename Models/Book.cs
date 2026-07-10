using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibWiseApp.Models;

public class Book
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string? ISBN { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Author { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Publisher { get; set; }

    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [Required, Range(1000, 2100)]
    public int PublicationYear { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Total copies must be at least 1.")]
    public int TotalCopies { get; set; } = 1;

    [Required, Range(0, int.MaxValue)]
    public int AvailableCopies { get; set; } = 1;

    [MaxLength(100)]
    public string? ShelfLocation { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BorrowingRecord> BorrowingRecords { get; set; } = new List<BorrowingRecord>();
    public ICollection<BookStatusLog> StatusLogs { get; set; } = new List<BookStatusLog>();
}
