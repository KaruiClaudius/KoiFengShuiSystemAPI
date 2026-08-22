using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.FengShui.Domain.Entities;

public class PartnerShop
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [Required]
    [MaxLength(500)]
    [Url]
    public string LinkUrl { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
