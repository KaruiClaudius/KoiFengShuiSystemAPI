using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.Identity.Application.Requests;

public class ChangePasswordRequest
{
    [Required]
    public string? CurrentPassword { get; set; }

    [Required]
    [MinLength(6)]
    public string? NewPassword { get; set; }
}
