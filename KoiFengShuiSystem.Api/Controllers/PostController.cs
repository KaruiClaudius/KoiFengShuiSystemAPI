using Azure;
using Azure.Core;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Drawing.Printing;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public class PostController : Controller
    {
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
        //[Authorize]
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

        //[Authorize]
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var postResponse = await _postService.DeletePost(id);
            if (postResponse.Data == null)
            {
                return BadRequest(postResponse.Message);
            }
            return Ok(postResponse);
        }
        [HttpPost("Create")]
        //[Authorize=]
        public async Task<IActionResult> CreateAsync([FromBody] Post posts)
        {
            var postResponse = await _postService.CreatePost(posts);
            if (postResponse.Data != null)
            {
                return BadRequest(postResponse);
            }
            return Ok(postResponse);
        }
    }
}
