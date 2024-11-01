using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubcriptionTiersController : Controller
    {
        private ISubcriptionTiersService _subcriptionTiersService;
        public SubcriptionTiersController(ISubcriptionTiersService subcriptionTiersService)
        {
            _subcriptionTiersService = subcriptionTiersService;
        }
        [HttpGet("GetAll")]
        public async Task<IBusinessResult> GetAll()
        {
            /*if (id == 0)
            {
                return NotFound("Value must be passed in request body");
            }*/
            return await _subcriptionTiersService.GetAllSubcriptionTiers();
        }
    }
}
