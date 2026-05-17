using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class PostCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string PostType { get; set; } = string.Empty;

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}
