using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class FAQController : ControllerBase
    {
        private readonly IFAQService _faqService;

        public FAQController(IFAQService faqService)
        {
            _faqService = faqService;
        }

        [HttpGet("{GetAllFAQ}")]
        public async Task<IActionResult> GetAll()
        {
            var faqs = await _faqService.GetAllFAQsAsync();
            return Ok(faqs);
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var faq = await _faqService.GetFAQByIdAsync(id);
            if (faq == null) return NotFound();
            return Ok(faq);
        }

        [HttpPost("{CreateFAQ}")]
        public async Task<IActionResult> Create([FromBody] FAQRequest faqRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var faq = await _faqService.CreateFAQAsync(faqRequest);
            return CreatedAtAction(nameof(GetById), new { id = faq.FAQId }, faq);
        }

        [HttpPut("{UpdateFAQ}")]
        public async Task<IActionResult> Update(int id, [FromBody] FAQRequest faqRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var faq = await _faqService.UpdateFAQAsync(id, faqRequest);
            if (faq == null) return NotFound();
            return Ok(faq);
        }

        [HttpDelete("{UpdateFAQ}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _faqService.DeleteFAQAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
