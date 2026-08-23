using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace KoiFengShuiSystem.Modules.Community.Infrastructure.CloudStorage
{
    /// <summary>
    /// Port of the legacy <c>CloudService</c> onto the module-owned
    /// <see cref="ICloudStorage"/> abstraction. Validation and error-wrapping
    /// behavior is replicated exactly: null/empty/non-PNG-JPEG files throw,
    /// provider transport failures are rethrown wrapped, and provider-level
    /// rejections come back as an unsuccessful <see cref="CloudUploadResult"/>.
    /// </summary>
    public class CloudStorageService : ICloudStorage
    {
        private readonly Cloudinary _cloudinary;

        public CloudStorageService(IOptions<CloudStorageSettings> settingsOptions)
        {
            var settings = settingsOptions.Value;

            var account = new Account(
                settings.CloudName,
                settings.ApiKey,
                settings.ApiSecret);

            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<CloudUploadResult> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0 ||
                (file.ContentType != "image/png" && file.ContentType != "image/jpeg"))
            {
                throw new Exception("File is null, empty, or not in PNG or JPEG format.");
            }

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    UploadPreset = "KoiFengShui"
                };

                try
                {
                    var result = await _cloudinary.UploadAsync(uploadParams);
                    return result.Error == null
                        ? new CloudUploadResult(true, result.SecureUrl.ToString(), null)
                        : new CloudUploadResult(false, null, result.Error.Message);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to upload image to Cloudinary.", ex);
                }
            }
        }
    }
}
