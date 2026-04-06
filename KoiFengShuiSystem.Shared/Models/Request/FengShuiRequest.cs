using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class FengShuiRequest
    {
        [Required(ErrorMessage = "Year of birth is required")]
        [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100")]
        public int YearOfBirth { get; set; }

        public bool IsMale { get; set; }
    }
}
