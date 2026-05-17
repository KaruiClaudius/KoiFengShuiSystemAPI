using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class Direction
{
    [Key]
    public int DirectionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DirectionName { get; set; } = string.Empty;

    public virtual ICollection<FengShuiDirection> FengShuiDirections { get; set; } = new List<FengShuiDirection>();
}
