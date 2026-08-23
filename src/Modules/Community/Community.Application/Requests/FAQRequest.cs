using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Modules.Community.Application.Requests
{
    public class FAQRequest
    {
        [Required]
        [MaxLength(1000)]
        public string Question { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Answer { get; set; } = string.Empty;

        public int AccountId { get; set; }
    }
}
