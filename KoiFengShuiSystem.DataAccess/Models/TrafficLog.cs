using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class TrafficLog
{
    [Key]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public bool IsRegistered { get; set; }

    public int? AccountId { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(255)]
    public string? UserAgent { get; set; }

    [MaxLength(255)]
    public string? RequestPath { get; set; }

    [MaxLength(10)]
    public string? RequestMethod { get; set; }

    public virtual Account? Account { get; set; }
}
