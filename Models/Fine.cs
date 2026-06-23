using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibWiseApp.Models;

public class Fine
{
    public int Id { get; set; }

    public int BorrowingRecordId { get; set; }

    [ForeignKey(nameof(BorrowingRecordId))]
    public BorrowingRecord BorrowingRecord { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    [MaxLength(450)]
    public string? PaidByUserId { get; set; }

    [ForeignKey(nameof(PaidByUserId))]
    public ApplicationUser? PaidBy { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Unpaid";

    public string? Remarks { get; set; }
}
