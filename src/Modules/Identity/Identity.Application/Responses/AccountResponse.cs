namespace KoiFengShuiSystem.Modules.Identity.Application.Responses;

public class AccountResponse
{
    public int AccountId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime Dob { get; set; }

    public string? Phone { get; set; }

    public string? Gender { get; set; }

    public int? RoleId { get; set; }

    /// <summary>
    /// Ngũ-hành element id derived from birth year/gender (council Q7); null for
    /// accounts without a derivable element. Pair with <see cref="ElementName"/>.
    /// </summary>
    public int? ElementId { get; set; }

    public string? ElementName { get; set; }
}
