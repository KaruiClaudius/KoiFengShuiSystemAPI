using System.ComponentModel.DataAnnotations;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.FengShui.Domain.Entities;

public class Element
{
    [Key]
    public int ElementId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ElementName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LuckyNumber { get; set; } = string.Empty;

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<FengShuiDirection> FengShuiDirections { get; set; } = new List<FengShuiDirection>();

    public virtual ICollection<KoiBreed> KoiBreeds { get; set; } = new List<KoiBreed>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<ShapeCategory> ShapeCategories { get; set; } = new List<ShapeCategory>();
}
