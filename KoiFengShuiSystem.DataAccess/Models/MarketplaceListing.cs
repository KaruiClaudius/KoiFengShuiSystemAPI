using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoiFengShuiSystem.DataAccess.Models;

public class MarketplaceListing
{
    [Key]
    public int ListingId { get; set; }

    public int AccountId { get; set; }

    public int TierId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(20)]
    public string? Color { get; set; }

    public int Quantity { get; set; }

    public int CategoryId { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public int? ElementId { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual MarketCategory Category { get; set; } = null!;

    public virtual Element? Element { get; set; }

    public virtual ICollection<ListingImage> ListingImages { get; set; } = new List<ListingImage>();

    public virtual SubcriptionTier Tier { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
