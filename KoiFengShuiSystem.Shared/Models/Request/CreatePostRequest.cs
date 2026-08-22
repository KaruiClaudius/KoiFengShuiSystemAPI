using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    /// <summary>
    /// Client-facing contract for member post creation. Deliberately excludes
    /// server-owned fields (Status, CreateAt/UpdateAt, PostId, AccountId) so
    /// clients can never mass-assign them; the service layer sets defaults.
    /// </summary>
    public class CreatePostRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        public List<int>? ImageIds { get; set; }
    }
}
