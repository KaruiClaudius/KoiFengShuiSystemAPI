using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public class MarketplaceListingsController : Controller
    {
        private IMarketplaceListingService _marketplaceListingService;
        private readonly ILogger<MarketplaceListingsController> _logger;
        public MarketplaceListingsController(IMarketplaceListingService marketplaceListingService, ILogger<MarketplaceListingsController> logger)
        {
            _marketplaceListingService = marketplaceListingService;
            _logger = logger;
        }
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var marketplaceListingResponse = await _marketplaceListingService.GetAll();

            if (marketplaceListingResponse.Data == null)
            {
                return NotFound(marketplaceListingResponse);
            }
            return Ok(marketplaceListingResponse);
        }

        [HttpGet("GetAllByCategoryType/{categoryId}")]
        public async Task<IActionResult> GetMarketplaceListingByCategoryId(int categoryId, [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            var marketplaceListingResponse = await _marketplaceListingService.GetMarketplaceListingByCategoryId(categoryId, page, pageSize);
            if (marketplaceListingResponse.Data == null)
            {
                return NotFound(marketplaceListingResponse);
            }
            return Ok(marketplaceListingResponse);
        }

        //[Authorize]
        [HttpGet("GetAllByElementId/{elementId}/Category/{categoryId}")]
        public async Task<IActionResult> GetMarketplaceListingByElement(int elementId, int categoryId, [FromQuery] int excludeListingId, [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            var marketplaceListingResponse = await _marketplaceListingService.GetMarketplaceListingByElementId(elementId, categoryId, excludeListingId, page, pageSize);
            if (marketplaceListingResponse.Data == null)
            {
                return NotFound(marketplaceListingResponse);
            }
            return Ok(marketplaceListingResponse);
        }

        //[Authorize]
        [HttpGet("GetAllByAccount/{accountId}/Category/{categoryId}")]
        public async Task<IActionResult> GetMarketplaceListingByAccount(int accountId, int categoryId, [FromQuery] int excludeListingId, [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            var marketplaceListingResponse = await _marketplaceListingService.GetMarketplaceListingByAccountId(accountId, categoryId, excludeListingId, page, pageSize);
            if (marketplaceListingResponse.Data == null)
            {
                return NotFound(marketplaceListingResponse);
            }
            return Ok(marketplaceListingResponse);
        }

        //[Authorize]
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var marketplaceListingResponse = await _marketplaceListingService.GetMarketplaceListingById(id);
            if (marketplaceListingResponse.Data == null)
            {
                return NotFound(marketplaceListingResponse);
            }
            return Ok(marketplaceListingResponse);
        }

        //[Authorize]
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var marketplaceListingResponse = await _marketplaceListingService.DeleteMarketplaceListing(id);
            if (marketplaceListingResponse.Data == null)
            {
                return BadRequest(marketplaceListingResponse.Message);
            }
            return Ok(marketplaceListingResponse);
        }
        [HttpPost("Create")]
        //[Authorize=]
        public async Task<IActionResult> CreateAsync([FromBody] MarketplaceListing marketplaceListing)
        {
            var marketplaceListingResponse = await _marketplaceListingService.CreateMarketplaceListing(marketplaceListing);
            if (marketplaceListingResponse.Data != null)
            {
                return BadRequest(marketplaceListingResponse);
            }
            return Ok(marketplaceListingResponse);
        }
    }
}
