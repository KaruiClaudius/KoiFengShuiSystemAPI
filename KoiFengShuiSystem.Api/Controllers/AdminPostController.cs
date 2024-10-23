using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminPostController : Controller
    {
        private IAdminPostService _adminPostService;
        private readonly ILogger<AdminPostController> _logger;

        public AdminPostController(IAdminPostService adminPostService, ILogger<AdminPostController> logger)
        {
            _adminPostService = adminPostService;
            _logger = logger;
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var postResponse = await _adminPostService.GetPostByIdAsync(id);
            if (postResponse == null)
            {
                return NotFound(postResponse);
            }
            return Ok(postResponse);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateAsync([FromBody] PostRequest postRequest)
        {
            var postResponse = await _adminPostService.CreatePostAsync(postRequest);
            if (postResponse == null)
            {
                return BadRequest(postResponse);
            }
            return Ok(postResponse);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PostRequest postRequest)
        {
            var postResponse = await _adminPostService.UpdatePostAsync(id, postRequest);
            if (postResponse == null)
            {
                return NotFound(postResponse);
            }
            return Ok(postResponse);
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var postResponse = await _adminPostService.DeletePostAsync(id);
            if (!postResponse)
            {
                return BadRequest("Error deleting Post.");
            }
            return Ok("Post deleted successfully.");
        }

        // Get posts where role = 1
        [HttpGet("GetAllAdminPosts")]
        public async Task<IActionResult> GetAllAdminPosts()
        {
            try
            {
                var adminPosts = await _adminPostService.GetAllAdminPostsAsync();
                if (adminPosts == null || !adminPosts.Any())
                {
                    return NotFound("No admin posts found.");
                }
                return Ok(adminPosts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching admin posts");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}