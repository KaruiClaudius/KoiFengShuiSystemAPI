using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KoiFengShuiSystem.Modules.FengShui.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultationController : Controller
    {
        private readonly IConsultationService _consultationService;
        public ConsultationController(IConsultationService consultationService)
        {
            _consultationService = consultationService;
        }
        [EnableRateLimiting("compute")]
        [HttpPost("fengshui")]
        public async Task<IActionResult> GetFengShuiConsultation([FromBody] FengShuiRequest request)
        {
            if (request.YearOfBirth <= 0)
            {
                return BadRequest("Year of birth must be a positive number.");
            }

            try
            {
                var response = await _consultationService.GetFengShuiConsultationAsync(
                    request.YearOfBirth,
                    request.IsMale
                );
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
