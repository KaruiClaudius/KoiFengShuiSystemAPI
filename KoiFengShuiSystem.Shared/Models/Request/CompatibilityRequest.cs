using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class CompatibilityRequest
    {
        [Required(ErrorMessage = "Date of birth (year) is required")]
        [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100")]
        public int DateOfBirth { get; set; }

        public bool IsMale { get; set; }

        [Required(ErrorMessage = "Direction is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Direction must be between 1 and 50 characters")]
        public string Direction { get; set; }

        [Required(ErrorMessage = "At least one fish color is required")]
        [MinLength(1, ErrorMessage = "At least one fish color is required")]
        public List<string> FishColors { get; set; }

        [Range(1, 999, ErrorMessage = "Fish quantity must be between 1 and 999")]
        public int FishQuantity { get; set; }

        [Required(ErrorMessage = "Pond shape is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Pond shape must be between 1 and 50 characters")]
        public string PondShape { get; set; }
    }
}
