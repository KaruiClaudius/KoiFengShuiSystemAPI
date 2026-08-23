using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.Identity.Application.Requests;

public class ResetPasswordRequest
{
    [Required]
    public string? Token { get; set; }

    [Required]
    [MinLength(8)]
    public string? NewPassword { get; set; }
}
