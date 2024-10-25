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
    [Route("api/[controller]")]
    public class FAQController : Controller
    {
        private IFAQService _faqService;
        private readonly ILogger<FAQController> _logger;

        public FAQController(IFAQService faqService, ILogger<FAQController> logger)
        {
            _faqService = faqService;
            _logger = logger;
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
        public async Task<IActionResult> CreateAsync([FromBody] FAQRequest faqRequest)
        {
            var faqResponse = await _faqService.CreateFAQAsync(faqRequest);
            if (faqResponse == null)
            {
                return BadRequest(faqResponse);
            }
            return Ok(faqResponse);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FAQRequest faqRequest)
        {
            var faqResponse = await _faqService.UpdateFAQAsync(id, faqRequest);
            if (faqResponse == null)
            {
                return NotFound(faqResponse);
            }
            return Ok(faqResponse);
        }

        [HttpDelete("Delete/{id}")]
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
