using KoiFengShuiSystem.DataAccess.Models;

namespace KoiFengShuiSystem.Modules.Community.Application.Responses
{
    public class PostResponse
    {
        public int PostId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public int AccountId { get; set; }
        /// <summary>
        /// Interim sentinel mapping: 0 means uncategorized (member post created
        /// without an element). Planned to become <c>int?</c> once callers can
        /// handle a null element explicitly instead of this sentinel value.
        /// </summary>
        public int ElementId { get; set; }
        public string Status { get; set; }
        public string ElementName { get; set; } // Added ElementName here
        public string AccountName { get; set; } // Added Account Name
        public ICollection<Follow> Follows { get; set; }

        /// <summary>
        /// Image urls attached to the post (council D11: public blog detail renders
        /// in one call). Empty when the post has no images.
        /// </summary>
        public List<string> ImageUrls { get; set; } = [];
    }
}
