using Microsoft.AspNetCore.Http;

namespace KoiFengShuiSystem.Modules.Community.Application.Abstractions
{
    /// <summary>Module-owned result of a cloud image upload.</summary>
    /// <param name="Success">True when the provider accepted and stored the file.</param>
    /// <param name="Url">Secure public url of the stored image; null on failure.</param>
    /// <param name="Error">Provider rejection message; null on success.
    /// Transport or validation failures surface as exceptions instead,
    /// mirroring the legacy <c>ICloudService</c> contract.</param>
    public sealed record CloudUploadResult(bool Success, string? Url, string? Error);

    /// <summary>
    /// Ported from the legacy <c>ICloudService</c>. Implementations wrap the
    /// configured cloud storage provider (Cloudinary) and reject anything
    /// except PNG/JPEG uploads by throwing, exactly as the legacy service did.
    /// </summary>
    public interface ICloudStorage
    {
        /// <summary>Uploads one image and reports success plus its secure url.</summary>
        Task<CloudUploadResult> UploadImageAsync(IFormFile file);
    }
}
