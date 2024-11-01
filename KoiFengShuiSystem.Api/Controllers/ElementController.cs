using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.DataAccess.Base;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElementController : Controller
    {
        private IElementService _elementService;
        public ElementController(IElementService elementService  )
        {
            _elementService= elementService;
        }
        [HttpGet("GetAll")]
        public async Task<IBusinessResult> GetAll()
        {
            /*if (id == 0)
            {
                return NotFound("Value must be passed in request body");
            }*/
            return await _elementService.GetAllElement();
        }
    }
}
