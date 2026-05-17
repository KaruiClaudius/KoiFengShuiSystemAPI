using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class ListingImage
{
    [Key]
    public int ListingImageId { get; set; }

    public int MarketListingId { get; set; }

    public int ImageId { get; set; }

    public string? ImageDescription { get; set; }

    public virtual Image Image { get; set; } = null!;

    public virtual MarketplaceListing MarketListing { get; set; } = null!;
}
