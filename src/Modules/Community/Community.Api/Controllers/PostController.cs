using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Services;
using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace KoiFengShuiSystem.Modules.Community.Api.Controllers
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
            // Council D2: public detail is Approved-only; admins read the full
            // queue through the dedicated bypass instead of the filtered path.
            var isAdmin = User.IsInRole(AuthorizationDefaults.Roles.Admin);
            var postResponse = isAdmin
                ? await _postService.GetPostByIdForAdmin(id)
                : await _postService.GetPostById(id);

            if (postResponse.Data == null)
            {
                return NotFound(postResponse);
            }
            return Ok(postResponse);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categoriesResponse = await _postService.GetCategories();
            if (categoriesResponse.Data == null)
            {
                return NotFound(categoriesResponse);
            }
            return Ok(categoriesResponse);
        }

        [HttpGet("my-posts")]
        [Authorize]
        public async Task<IActionResult> GetMyPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            // Council Q11: identity strictly from the token - a spoofed account id
            // cannot widen the result. accountId 0 (missing claim) reads empty.
            var authorAccountId = ParseAccountId(User);
            var postResponse = await _postService.GetMyPosts(authorAccountId, page, pageSize);

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
