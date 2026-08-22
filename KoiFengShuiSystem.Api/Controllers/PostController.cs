using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Kernel.Results;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Kernel.Security;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Drawing.Printing;
using System.Security.Claims;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : Controller
    {
        /// <summary>Claim type carrying the account id minted by JwtTokenService.</summary>
        private const string AccountIdClaim = "id";

        private IPostService _postService;
        private readonly ILogger<PostController> _logger;
        public PostController(IPostService postService, ILogger<PostController> logger)
        {
            _postService = postService;
            _logger = logger;
        }
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var postResponse = await _postService.GetAll();

            if (postResponse.Data == null)
            {
                return NotFound(postResponse);
            }
            return Ok(postResponse);
        }

        [HttpGet("GetAllByPostType/{postTypeId}")]
        public async Task<IActionResult> GetByPostTypeId(int postTypeId, [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            var postResponse = await _postService.GetPostByPostTypeId(postTypeId, page, pageSize);
            if (postResponse.Data == null)
            {
                return NotFound(postResponse);
            }
            return Ok(postResponse);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var postResponse = await _postService.GetPostById(id);
            if (postResponse.Data == null)
            {
                return NotFound(postResponse);
            }
            return Ok(postResponse);
        }

        // No per-account ownership check exists in PostService.DeletePost yet,
        // so deletion is restricted to admins until author tracking lands.
        [HttpDelete("Delete/{id}")]
        [Authorize(Roles = AuthorizationDefaults.Roles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var postResponse = await _postService.DeletePost(id);
            if (postResponse.Data == null && !postResponse.Success)
            {
                return BadRequest(postResponse.Message);
            }
            return Ok(postResponse);
        }

        [HttpPost("Create")]
        [Authorize]
        public async Task<IActionResult> CreateAsync([FromBody] CreatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var authorAccountId = ParseAccountId(User);
            var postResponse = await _postService.CreatePost(request, authorAccountId);

            if (!postResponse.Success)
            {
                return BadRequest(postResponse.Message);
            }
            return Ok(postResponse);
        }

        private static int ParseAccountId(ClaimsPrincipal? user)
        {
            if (user is null)
            {
                return 0;
            }

            return int.TryParse(user.FindFirstValue(AccountIdClaim), out var accountId) ? accountId : 0;
        }
    }
}
