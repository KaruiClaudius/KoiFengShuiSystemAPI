using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.Identity.Application.Requests;

public class RegisterRequest
{
    [Required]
    public string? FullName { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MinLength(6)]
    public string? Password { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Dob { get; set; }

    [Required]
    public string? Phone { get; set; }

    [Required]
    public string? Gender { get; set; }
}
