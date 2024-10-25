using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminPostController : ControllerBase
    {
        private readonly IAdminPostService _adminPostService;
        private readonly ILogger<AdminPostController> _logger;

        public AdminPostController(
            IAdminPostService adminPostService,
            ILogger<AdminPostController> logger)
        {
            _adminPostService = adminPostService;
            _logger = logger;
        }

        [HttpGet("GetAllPosts")]
        public async Task<ActionResult<IEnumerable<AdminPostResponse>>> GetAllAdminPosts()
        {
            try
            {
                var posts = await _adminPostService.GetAllAdminPostsAsync();
                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all admin posts");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetPostById/{id}")]
        public async Task<ActionResult<AdminPostResponse>> GetAdminPostById(int id)
        {
            try
            {
                var post = await _adminPostService.GetAdminPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound();
                }
                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching admin post with id {id}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPut("UpdatePost/{id}")]
        public async Task<ActionResult<AdminPostResponse>> UpdateAdminPost(int id, [FromForm] AdminPostRequest request)
        {
            try
            {
                var post = await _adminPostService.UpdateAdminPostAsync(id, request, request.Images);
                if (post == null)
                {
                    return NotFound();
                }
                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating admin post with id {id}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("CreatePostWithImages")]
        public async Task<ActionResult<AdminPostResponse>> CreatePostWithImages([FromForm] AdminPostRequest request)
        {
            try
            {
                var post = await _adminPostService.CreatePostWithImagesAsync(request, request.Images);
                return CreatedAtAction(nameof(GetAdminPostById), new { id = post.PostId }, post);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid data provided for creating admin post");
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error occurred while saving admin post to database");
                return StatusCode(500, "An error occurred while saving the post. Please check if the provided data is valid.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating admin post with images");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpDelete("DeletePostWithAllRelated/{postId}")]
        public async Task<ActionResult> DeletePostWithAllRelated(int postId)
        {
            try
            {
                var result = await _adminPostService.DeletePostWithAllRelatedAsync(postId);
                if (!result)
                {
                    return NotFound();
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting post and all related data for post id {postId}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}