using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class Country
{
    [Key]
    public int CountryId { get; set; }

    [Required]
    [MaxLength(50)]
    public string CountryName { get; set; } = string.Empty;

    public virtual ICollection<KoiBreed> KoiBreeds { get; set; } = new List<KoiBreed>();
}
