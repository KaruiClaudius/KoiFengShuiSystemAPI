using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public class MarketplaceListingsController : Controller
    {
        private IMarketplaceListingService _marketplaceListingService;
        private readonly ILogger<MarketplaceListingsController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GenericRepository<Account> _accountRepository;
        public MarketplaceListingsController(IMarketplaceListingService marketplaceListingService, ILogger<MarketplaceListingsController> logger, IHttpContextAccessor httpContextAccessor, GenericRepository<Account> accountRepository)
        {
            _marketplaceListingService = marketplaceListingService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _accountRepository = accountRepository;
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
        public async Task<IActionResult> CreateAsync([FromForm] MarketplaceListingRequest marketplaceListing, [FromForm] List<IFormFile> images)
        {
            try
            {

                // Assuming you have user authentication and can get the user ID from claims
                var userEmail = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
                // or ClaimTypes.Email or whatever you use
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User not authenticated."); // Return 401 Unauthorized if user not found
                }
                var user = await _accountRepository.FindAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return NotFound("User not found");
                }
                var marketplaceListingResponse = await _marketplaceListingService.CreateMarketplaceListing(marketplaceListing, images, user.AccountId);
                if (marketplaceListingResponse.Data != null)
                {
                    return BadRequest(marketplaceListingResponse);
                }
                return Ok(marketplaceListingResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
