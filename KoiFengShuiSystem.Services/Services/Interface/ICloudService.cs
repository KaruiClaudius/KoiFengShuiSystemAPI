using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface ICloudService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file);

        Task<List<ImageUploadResult>> UploadImagesAsync(List<IFormFile> files);
    }
}
