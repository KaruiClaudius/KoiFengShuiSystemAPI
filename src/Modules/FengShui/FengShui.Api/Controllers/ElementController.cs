using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using KoiFengShuiSystem.Shared.Kernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Modules.FengShui.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElementController : Controller
    {
        private IElementService _elementService;
        public ElementController(IElementService elementService)
        {
            _elementService = elementService;
        }
        [HttpGet("GetAll")]
        public async Task<IBusinessResult> GetAll()
        {
            return await _elementService.GetAllElement();
        }
    }
}
