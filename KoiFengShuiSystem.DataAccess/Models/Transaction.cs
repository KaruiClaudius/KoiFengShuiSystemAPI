using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoiFengShuiSystem.DataAccess.Models;

public class Transaction
{
    [Key]
    public int Id { get; set; }

    public int TransactionId { get; set; }

    public int AccountId { get; set; }

    public int? TierId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public int? ListingId { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual MarketplaceListing? Listing { get; set; }

    public virtual SubcriptionTier? Tier { get; set; }
}
