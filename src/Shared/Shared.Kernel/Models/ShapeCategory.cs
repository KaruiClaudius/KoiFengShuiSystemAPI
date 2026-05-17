using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class ShapeCategory
{
    [Key]
    public int ShapeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ShapeName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Description { get; set; } = string.Empty;

    public int? ElementId { get; set; }

    public virtual Element? Element { get; set; }

    public virtual ICollection<FishPond> FishPonds { get; set; } = new List<FishPond>();
}
