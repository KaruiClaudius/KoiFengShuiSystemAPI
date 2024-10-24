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
        private readonly IAdminPostService _adminPostService;
        private readonly ILogger<AdminPostController> _logger;

        public AdminPostController(IAdminPostService adminPostService, ILogger<AdminPostController> logger)
        {
            _adminPostService = adminPostService;
            _logger = logger;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var postResponse = await _adminPostService.GetAllPostsAsync();

            if (postResponse == null || !postResponse.Any())
            {
                return NotFound("No posts found.");
            }
            return Ok(postResponse);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var postResponse = await _adminPostService.GetPostByIdAsync(id);
            if (postResponse == null)
            {
                return NotFound("Post not found.");
            }
            return Ok(postResponse);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateAsync([FromBody] PostRequest postRequest)
        {
            var postResponse = await _adminPostService.CreatePostAsync(postRequest);
            if (postResponse == null)
            {
                return BadRequest("Failed to create post.");
            }
            return CreatedAtAction(nameof(GetById), new { id = postResponse.PostId }, postResponse);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PostRequest postRequest)
        {
            var postResponse = await _adminPostService.UpdatePostAsync(id, postRequest);
            if (postResponse == null)
            {
                return NotFound("Post not found.");
            }
            return Ok(postResponse);
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _adminPostService.DeletePostAsync(id);
            if (!result)
            {
                return NotFound("Post not found.");
            }
            return Ok("Post deleted successfully.");
        }

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