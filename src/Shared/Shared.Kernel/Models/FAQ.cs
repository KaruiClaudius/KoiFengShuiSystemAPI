using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class FAQ
{
    [Key]
    public int FAQId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string Answer { get; set; } = string.Empty;

    public DateTime CreateAt { get; set; }

    public int AccountId { get; set; }

    public virtual Account Account { get; set; } = null!;
}
