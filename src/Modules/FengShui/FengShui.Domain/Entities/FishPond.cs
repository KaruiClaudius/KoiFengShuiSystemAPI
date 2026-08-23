using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.FengShui.Domain.Entities;

public class FishPond
{
    [Key]
    public int PondId { get; set; }

    public int ShapeId { get; set; }

    public int DirectionPlacement { get; set; }

    public virtual FengShuiDirection DirectionPlacementNavigation { get; set; } = null!;

    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();

    public virtual ShapeCategory Shape { get; set; } = null!;
}
