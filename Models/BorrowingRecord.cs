using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibWiseApp.Models;

public class BorrowingRecord
{
    public int Id { get; set; }

    public int BookId { get; set; }

    [ForeignKey(nameof(BookId))]
    public Book Book { get; set; } = null!;

    public int BorrowerId { get; set; }

    [ForeignKey(nameof(BorrowerId))]
    public Borrower Borrower { get; set; } = null!;

    [MaxLength(50)]
    public string? BorrowerBarcode { get; set; }

    [Required, MaxLength(450)]
    public string BorrowedByUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(BorrowedByUserId))]
    public ApplicationUser? BorrowedBy { get; set; }

    public DateTime BorrowedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }

    [MaxLength(450)]
    public string? ReturnedByUserId { get; set; }

    [ForeignKey(nameof(ReturnedByUserId))]
    public ApplicationUser? ReturnedBy { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    public bool WasExtended { get; set; } = false;

    public string? Remarks { get; set; }

    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
}
