using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class KoiBreed
{
    [Key]
    public int BreedId { get; set; }

    public int ElementId { get; set; }

    public int CountryId { get; set; }

    [Required]
    [MaxLength(50)]
    public string BreedName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Color { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Description { get; set; } = string.Empty;

    public virtual Country Country { get; set; } = null!;

    public virtual Element Element { get; set; } = null!;

    public virtual ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
}
