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
        private readonly ICloudService _cloudService;
        public AdminPostController(
            IAdminPostService adminPostService,
            ICloudService cloudService,
            ILogger<AdminPostController> logger)
        {
            _adminPostService = adminPostService;
            _cloudService = cloudService;
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
                var imageUrls = new List<string>();
                if (request.Images != null && request.Images.Any())
                {
                    foreach (var image in request.Images)
                    {
                        var uploadResult = await _cloudService.UploadImageAsync(image);
                        if (uploadResult.Error != null)
                        {
                            throw new Exception("Error uploading image: " + uploadResult.Error.Message);
                        }
                        imageUrls.Add(uploadResult.SecureUrl.ToString());
                    }
                }         
                var post = await _adminPostService.UpdateAdminPostAsync(id, request, imageUrls);
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
                var imageUrls = new List<string>();
                if (request.Images != null && request.Images.Any())
                {
                    foreach (var image in request.Images)
                    {
                        var uploadResult = await _cloudService.UploadImageAsync(image);
                        if (uploadResult.Error != null)
                        {
                            throw new Exception("Error uploading image: " + uploadResult.Error.Message);
                        }
                        imageUrls.Add(uploadResult.SecureUrl.ToString());
                    }
                }          
                var post = await _adminPostService.CreatePostWithImagesAsync(request, imageUrls);
                return CreatedAtAction(nameof(GetAdminPostById), new { id = post.PostId }, post);
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