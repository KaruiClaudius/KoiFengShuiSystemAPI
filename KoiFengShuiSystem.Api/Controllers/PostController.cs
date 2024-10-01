using Azure;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public class PostController : Controller
    {
        private IPostService _postService;
        private readonly ILogger<PostService> _logger;
        public PostController(IPostService postService, ILogger<PostService> logger)
        {
            _postService = postService;
            _logger = logger;
        }
        [HttpGet]
        public IActionResult GetAll()
        {
            var posts = _postService.GetAll();
            return Ok(posts);
        }
        //[Authorize]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var post = _postService.GetById(id);
            return post == null ? NotFound() : Ok(post);
        }

        //[Authorize]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                _postService.Delete(id);
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost]
        //[Authorize=]
        public async Task<Post> CreateAsync(Post posts)
        {
           return await _postService.CreateAsync(posts);
        }
    }
}
