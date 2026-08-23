using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Modules.FengShui.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompatibilityController : Controller
    {
        private readonly ICompatibilityService _compatibilityService;

        public CompatibilityController(ICompatibilityService compatibilityService)
        {
            _compatibilityService = compatibilityService;
        }

        [HttpPost("lookup")]
        public async Task<IActionResult> AssessCompatibility([FromBody] CompatibilityRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _compatibilityService.AssessCompatibility(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}
