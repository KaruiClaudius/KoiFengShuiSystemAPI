using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketCategoryController : Controller
    {
        private IMarketCategoryService _marketCategoryService;
        public MarketCategoryController(IMarketCategoryService marketCategoryService)
        {
            _marketCategoryService = marketCategoryService;
        }
        [HttpGet("GetAll")]
        public async Task<IBusinessResult> GetAll()
        {
            /*if (id == 0)
            {
                return NotFound("Value must be passed in request body");
            }*/
            return await _marketCategoryService.GetAllMarketCategory();
        }
    }
}
