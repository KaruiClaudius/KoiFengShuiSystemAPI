namespace KoiFengShuiSystem.Modules.Community.Application.Responses
{
    /// <summary>
    /// Ported from the legacy misspelled <c>UploadRespose</c> shared model;
    /// property names (and therefore the serialized payload) are unchanged.
    /// </summary>
    public class UploadImageResponse
    {
        public string? Url { get; set; }
        public string? Message { get; set; }
    }
}
