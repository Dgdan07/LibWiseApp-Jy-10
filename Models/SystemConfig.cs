using System.ComponentModel.DataAnnotations;

namespace LibWiseApp.Models;

public class SystemConfig
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
