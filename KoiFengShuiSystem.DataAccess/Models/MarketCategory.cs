using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class MarketCategory
{
    [Key]
    public int Categoryid { get; set; }

    [Required]
    [MaxLength(20)]
    public string CategoryName { get; set; } = string.Empty;

    public virtual ICollection<MarketplaceListing> MarketplaceListings { get; set; } = new List<MarketplaceListing>();
}
