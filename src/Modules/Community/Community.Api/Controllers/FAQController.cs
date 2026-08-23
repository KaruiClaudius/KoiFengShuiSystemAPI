using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Services;
using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Modules.Community.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FAQController : Controller
    {
        private readonly IFaqService _faqService;

        public FAQController(IFaqService faqService)
        {
            _faqService = faqService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var faqResponse = await _faqService.GetAllFAQsAsync();

            if (faqResponse == null)
            {
                return NotFound(faqResponse);
            }
            return Ok(faqResponse);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var faqResponse = await _faqService.GetFAQByIdAsync(id);
            if (faqResponse == null)
            {
                return NotFound(faqResponse);
            }
            return Ok(faqResponse);
        }

        [HttpPost("Create")]
        [Authorize(Roles = AuthorizationDefaults.Roles.Admin)]
        public async Task<IActionResult> Create([FromBody] FAQRequest faqRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var faqResponse = await _faqService.CreateFAQAsync(faqRequest);
            if (faqResponse == null)
            {
                return BadRequest(faqResponse);
            }
            return Ok(faqResponse);
        }

        [HttpPut("Update/{id}")]
        [Authorize(Roles = AuthorizationDefaults.Roles.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] FAQRequest faqRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var faqResponse = await _faqService.UpdateFAQAsync(id, faqRequest);
            if (faqResponse == null)
            {
                return NotFound(faqResponse);
            }
            return Ok(faqResponse);
        }

        [HttpDelete("Delete/{id}")]
        [Authorize(Roles = AuthorizationDefaults.Roles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var faqResponse = await _faqService.DeleteFAQAsync(id);
            if (!faqResponse)
            {
                return BadRequest("Error deleting FAQ.");
            }
            return Ok("FAQ deleted successfully.");
        }
    }
}
