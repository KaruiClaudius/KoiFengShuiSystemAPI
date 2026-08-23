using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.Identity.Application.Requests;

public class UpdateRequest
{
    [EmailAddress]
    public string? Email { get; set; }

    public string? FullName { get; set; }

    public DateTime? Dob { get; set; }

    public string? Gender { get; set; }

    public string? Phone { get; set; }
}
