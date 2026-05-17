using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class FengShuiDirection
{
    [Key]
    public int Id { get; set; }

    public int DirectionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Description { get; set; } = string.Empty;

    public int ElementId { get; set; }

    public virtual Direction Direction { get; set; } = null!;

    public virtual Element Element { get; set; } = null!;

    public virtual ICollection<FishPond> FishPonds { get; set; } = new List<FishPond>();
}
