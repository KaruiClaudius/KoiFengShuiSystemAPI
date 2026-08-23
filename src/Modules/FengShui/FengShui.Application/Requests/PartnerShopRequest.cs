using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Requests
{
    /// <summary>
    /// API contract for creating or updating a partner shop.
    /// String length limits are enforced at the entity level
    /// (<see cref="KoiFengShuiSystem.Modules.FengShui.Domain.Entities.PartnerShop"/>):
    /// Name ≤ 200, Address ≤ 500, LinkUrl ≤ 500, Note ≤ 1000.
    /// </summary>
    public class PartnerShopRequest
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Required(ErrorMessage = "LinkUrl is required")]
        [Url(ErrorMessage = "LinkUrl must be a valid URL")]
        public string LinkUrl { get; set; } = string.Empty;

        public string? Note { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
