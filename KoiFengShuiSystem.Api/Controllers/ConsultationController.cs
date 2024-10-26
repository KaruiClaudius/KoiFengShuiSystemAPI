using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Api.Controllers
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
