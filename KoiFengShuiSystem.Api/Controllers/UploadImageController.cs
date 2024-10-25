using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadImageController : ControllerBase
    {
        private readonly ICloudService _cloudService;
        private readonly IImageService _imageService;

        public UploadImageController(ICloudService cloudService, IImageService imageService)
        {
            _cloudService = cloudService;
            _imageService = imageService;
        }

        [HttpPost("UploadFile")]
        public async Task<IBusinessResult> UploadFile([FromForm] UploadFileRequest req)
        {
            if (req.File == null || req.File.Length == 0)
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }

            try
            {
                // Upload image to cloud storage
                var uploadFile = await _cloudService.UploadImageAsync(req.File);

                if (uploadFile.Error == null)
                {
                    // Save the image URL in the database
                    var saveResult = await _imageService.SaveImageAsync(uploadFile.SecureUrl.ToString());
                    if (saveResult)
                    {
                        return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, new UploadRespose
                        {
                            Url = uploadFile.SecureUrl.ToString(),
                        });

                        //return Ok(ApiResult<UploadRespose>.Succeed(new UploadRespose
                        //{
                        //    Url = uploadFile.SecureUrl.ToString(),
                        //    Message = "Upload file success"
                        //}));
                    }
                    else
                    {
                        return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
                    }
                }
                else
                {
                    return new BusinessResult(Const.FAIL_CREATE_CODE, "Upload file error: " + uploadFile.Error.Message);

                    //return BadRequest(ApiResult<UploadRespose>.Error(new UploadRespose
                    //{
                    //    Message = "Upload file error: " + uploadFile.Error.Message
                    //}));
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
        }
    }
}
