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

    public string? ElementName { get; set; }
}
