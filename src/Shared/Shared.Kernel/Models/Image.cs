using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class Image
{
    [Key]
    public int ImageId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ImageUrl { get; set; } = string.Empty;

    public virtual ICollection<ListingImage> ListingImages { get; set; } = new List<ListingImage>();

    public virtual ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();
}
