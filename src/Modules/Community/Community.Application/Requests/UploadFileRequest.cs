using Microsoft.AspNetCore.Http;

namespace KoiFengShuiSystem.Modules.Community.Application.Requests
{
    /// <summary>
    /// Ported from the legacy shared <c>UploadFileRequest</c>; form binding
    /// shape is unchanged.
    /// </summary>
    public class UploadFileRequest
    {
        public IFormFile? File { get; set; }
    }
}
