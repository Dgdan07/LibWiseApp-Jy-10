using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace LibWiseApp.Models;

public class Borrower
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Remote("CheckBarcode", "Borrowers", "Librarian", HttpMethod = "POST", ErrorMessage = "This barcode already exists.")]
    public string Barcode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(150), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20), Phone]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Grade { get; set; }

    [MaxLength(50)]
    public string? IDNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedByUserId { get; set; }

    public ICollection<BorrowingRecord> BorrowingRecords { get; set; } = new List<BorrowingRecord>();
}
