namespace KoiFengShuiSystem.Modules.Community.Application.Responses
{
    /// <summary>
    /// Ported from the legacy misspelled <c>UploadRespose</c> shared model.
    /// Council D9 added <see cref="ImageId"/> (additive; <see cref="Url"/> unchanged)
    /// so member post creation can reference uploaded images by id.
    /// </summary>
    public class UploadImageResponse
    {
        /// <summary>Generated Images-row key for the stored upload.</summary>
        public int ImageId { get; set; }

        public string? Url { get; set; }
        public string? Message { get; set; }
    }
}
