using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibWiseApp.Models;

public class FineRule
{
    public int Id { get; set; }

    public int DaysAllowed { get; set; } = 14;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DailyFineRate { get; set; } = 5.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaxFine { get; set; } = 500.00m;

    public bool IsActive { get; set; } = true;
}
