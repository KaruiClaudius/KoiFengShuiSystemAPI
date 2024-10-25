using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminPostController : ControllerBase
    {
        private readonly IAdminPostService _adminPostService;
        private readonly IImageService _imageService;
        private readonly IAdminPostImageService _adminPostImageService;
        private readonly ILogger<AdminPostController> _logger;

        public AdminPostController(
            IAdminPostService adminPostService,
            IImageService imageService,
            IAdminPostImageService adminPostImageService,
            ILogger<AdminPostController> logger)
        {
            _adminPostService = adminPostService;
            _imageService = imageService;
            _adminPostImageService = adminPostImageService;
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

        [HttpGet("GetAllImages")]
        public async Task<ActionResult<IEnumerable<ImageResponse>>> GetAllImages()
        {
            try
            {
                var images = await _imageService.GetAllImagesAsync();
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all images");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetImageById/{id}")]
        public async Task<ActionResult<ImageResponse>> GetImageById(int id)
        {
            try
            {
                var image = await _imageService.GetImageByIdAsync(id);
                if (image == null)
                {
                    return NotFound();
                }
                return Ok(image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching image with id {id}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("GetPostImages/{postId}")]
        public async Task<ActionResult<IEnumerable<AdminPostImageResponse>>> GetAdminPostImages(int postId)
        {
            try
            {
                var postImages = await _adminPostImageService.GetAdminPostImagesByPostIdAsync(postId);
                return Ok(postImages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching images for post with id {postId}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("CreatePost")]
        public async Task<ActionResult<AdminPostResponse>> CreateAdminPost([FromForm] AdminPostRequest request)
        {
            try
            {
                var post = await _adminPostService.CreateAdminPostAsync(request, request.Images);
                return CreatedAtAction(nameof(GetAdminPostById), new { id = post.PostId }, post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating admin post");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("CreateImage")]
        public async Task<ActionResult<ImageResponse>> CreateImage([FromBody] ImageRequest request)
        {
            try
            {
                var image = await _imageService.CreateImageAsync(request);
                return CreatedAtAction(nameof(GetImageById), new { id = image.ImageId }, image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating image");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("CreatePostImage")]
        public async Task<ActionResult<AdminPostImageResponse>> CreateAdminPostImage([FromBody] AdminPostImageRequest request)
        {
            try
            {
                var postImage = await _adminPostImageService.CreateAdminPostImageAsync(request);
                return CreatedAtAction(nameof(GetAdminPostImages), new { postId = postImage.PostId }, postImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating post image");
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