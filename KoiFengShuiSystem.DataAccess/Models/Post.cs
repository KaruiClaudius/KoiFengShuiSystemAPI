using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class Post
{
    [Key]
    public int PostId { get; set; }

    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreateAt { get; set; }

    public DateTime UpdateAt { get; set; }

    public int AccountId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public int? ElementId { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Element? Element { get; set; }

    public virtual ICollection<Follow> Follows { get; set; } = new List<Follow>();

    public virtual PostCategory IdNavigation { get; set; } = null!;

    public virtual ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();
}
