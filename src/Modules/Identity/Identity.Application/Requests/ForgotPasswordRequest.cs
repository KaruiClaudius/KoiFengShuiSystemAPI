using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.Identity.Application.Requests;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
}
