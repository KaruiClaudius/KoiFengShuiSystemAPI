using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class SubcriptionTier
{
    [Key]
    public int TierId { get; set; }

    [Required]
    [MaxLength(255)]
    public string TierName { get; set; } = string.Empty;

    public virtual ICollection<MarketplaceListing> MarketplaceListings { get; set; } = new List<MarketplaceListing>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
