using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoiFengShuiSystem.DataAccess.Models;

public class Account
{
    [Key]
    public int AccountId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Password { get; set; }

    public DateTime? Dob { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(10)]
    public string? Gender { get; set; }

    public int? ElementId { get; set; }

    public int? RoleId { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime UpdateAt { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal? Wallet { get; set; }

    public virtual Element? Element { get; set; }

    public virtual Role? Role { get; set; }

    public virtual ICollection<FAQ> FAQs { get; set; } = new List<FAQ>();

    public virtual ICollection<Follow> Follows { get; set; } = new List<Follow>();

    public virtual ICollection<MarketplaceListing> MarketplaceListings { get; set; } = new List<MarketplaceListing>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();

    public virtual ICollection<TrafficLog> TrafficLogs { get; set; } = new List<TrafficLog>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
