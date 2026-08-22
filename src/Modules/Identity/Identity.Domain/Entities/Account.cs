using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.Identity.Domain.Entities;

public class Account
{
    [Key]
    public int AccountId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Password { get; set; }

    public DateTime? Dob { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(10)]
    public string? Gender { get; set; }

    public int? ElementId { get; set; }

    public int? RoleId { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime UpdateAt { get; set; }

    public virtual Role? Role { get; set; }
}
