using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.Identity.Domain.Entities;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    public int AccountId { get; set; }

    /// <summary>
    /// SHA-256 hex digest of the raw refresh token. The raw token itself is never persisted.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(64)]
    public string? ReplacedByTokenHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
