using KoiFengShuiSystem.Shared.Kernel;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Responses;
using KoiFengShuiSystem.Shared.Kernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Modules.Community.Api.Controllers
{
    /// <summary>
    /// Port of the legacy KoiFengShuiSystem.Api UploadImageController. Route,
    /// authorization, and the IBusinessResult response envelope are replicated
    /// byte-for-byte so existing clients and security-matrix pins stay valid.
    /// (Route rename to a cleaner surface is deferred to the API-cleanup pass.)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UploadImageController : ControllerBase
    {
        private readonly ICloudStorage _cloudStorage;
        private readonly ICommunityStore _communityStore;

        public UploadImageController(ICloudStorage cloudStorage, ICommunityStore communityStore)
        {
            _cloudStorage = cloudStorage;
            _communityStore = communityStore;
        }

        [HttpPost("UploadFile")]
        public async Task<IBusinessResult> UploadFile([FromForm] UploadFileRequest req)
        {
            if (req.File == null || req.File.Length == 0)
            {
                return new BusinessResult(ResponseCodes.FailCreateCode, ResponseCodes.FailCreateMessage);
            }

            try
            {
                // Upload image to cloud storage
                var uploadFile = await _cloudStorage.UploadImageAsync(req.File);

                if (uploadFile.Success)
                {
                    // Save the image URL in the database; the generated row key is
                    // part of the response (council D9 - imageId for post creation).
                    var imageId = await _communityStore.AddImageAsync(uploadFile.Url!);
                    return new BusinessResult(ResponseCodes.SuccessCreateCode, ResponseCodes.SuccessCreateMessage, new UploadImageResponse
                    {
                        ImageId = imageId,
                        Url = uploadFile.Url,
                    });
                }
                else
                {
                    return new BusinessResult(ResponseCodes.FailCreateCode, "Upload file error: " + uploadFile.Error);
                }
            }
            catch (Exception)
            {
                return new BusinessResult(ResponseCodes.FailCreateCode, ResponseCodes.FailCreateMessage);
            }
        }
    }
}
