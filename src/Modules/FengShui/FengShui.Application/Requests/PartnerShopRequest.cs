using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Requests
{
    public class PartnerShopRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address must be at most 500 characters")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "LinkUrl is required")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "LinkUrl must be between 1 and 500 characters")]
        [Url(ErrorMessage = "LinkUrl must be a valid URL")]
        public string LinkUrl { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Note must be at most 1000 characters")]
        public string? Note { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
