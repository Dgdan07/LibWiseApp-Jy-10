using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibWiseApp.Models;

public class BookStatusLog
{
    public int Id { get; set; }

    public int BookId { get; set; }

    [ForeignKey(nameof(BookId))]
    public Book Book { get; set; } = null!;

    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? ChangedByUserId { get; set; }

    [ForeignKey(nameof(ChangedByUserId))]
    public ApplicationUser? ChangedBy { get; set; }

    public string? Remarks { get; set; }
}
