using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class PostImage
{
    [Key]
    public int PostImageId { get; set; }

    public int PostId { get; set; }

    public int ImageId { get; set; }

    public string? ImageDescription { get; set; }

    public virtual Image Image { get; set; } = null!;

    public virtual Post Post { get; set; } = null!;
}
