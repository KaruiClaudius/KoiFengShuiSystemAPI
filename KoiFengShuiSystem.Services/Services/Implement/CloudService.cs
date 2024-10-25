using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.Shared.Helpers.Photos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class CloudService : ICloudService
    {
        private readonly Cloudinary _cloudinary;
        public CloudService(IOptions<CloundSettings> cloundSettingsOptions)
        {
            var cloudSettings = cloundSettingsOptions.Value;

            Account account = new Account(
                cloudSettings.CloundName,
                cloudSettings.CloundKey,
                cloudSettings.CloundSecret);

            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }
        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file)
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
                    return await _cloudinary.UploadAsync(uploadParams);
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi tải lên Cloudinary
                    throw new Exception("Failed to upload image to Cloudinary.", ex);
                }
            }
        }

        public async Task<List<ImageUploadResult>> UploadImagesAsync(List<IFormFile> files)
        {
            var uploadResults = new List<ImageUploadResult>();

            foreach (var file in files)
            {
                uploadResults.Add(await UploadImageAsync(file));
            }

            return uploadResults;
        }
    }
}
