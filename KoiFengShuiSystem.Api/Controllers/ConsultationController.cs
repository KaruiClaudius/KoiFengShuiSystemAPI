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
            var response = await _consultationService.GetFengShuiConsultationAsync(request.YearOfBirth);
            return Ok(response);
        }
    }
}
