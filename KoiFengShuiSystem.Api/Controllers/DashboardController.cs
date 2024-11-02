using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("new-users-count")]
        public async Task<IActionResult> GetNewUsersCount([FromQuery] int days = 30)
        {
            try
            {
                var count = await _dashboardService.CountNewUsersAsync(days);
                return Ok(new { Count = count });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("new-users-list")]
        public async Task<IActionResult> GetNewUsersList([FromQuery] int days = 30)
        {
            try
            {
                var users = await _dashboardService.ListNewUsersAsync(days);
                return Ok(users);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("traffic-distribution")]
        public async Task<IActionResult> GetTrafficDistribution()
        {
            try
            {
                var registeredUsers = await _dashboardService.GetRegisteredUsersTrafficCount();
                var uniqueGuests = await _dashboardService.GetUniqueGuestsTrafficCount();

                var total = registeredUsers + uniqueGuests;
                var registeredPercentage = (double)registeredUsers / total * 100;
                var uniqueGuestsPercentage = (double)uniqueGuests / total * 100;

                return Ok(new
                {
                    RegisteredUsers = Math.Round(registeredPercentage, 2),
                    UniqueGuests = Math.Round(uniqueGuestsPercentage, 2),
                    TotalVisitors = total
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("new-market-listings-count")]
        public async Task<IActionResult> GetNewMarketListingsCount([FromQuery] int days = 30)
        {
            try
            {
                var count = await _dashboardService.CountNewMarketListingsAsync(days);
                return Ok(new { Count = count });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("new-market-listings-by-category")]
        public async Task<IActionResult> GetNewMarketListingsByCategory([FromQuery] int days = 30)
        {
            try
            {
                var categoryCounts = await _dashboardService.CountNewMarketListingsByCategoryAsync(days);
                return Ok(categoryCounts);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("market-listings")]
        public async Task<IActionResult> GetMarketListings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var listings = await _dashboardService.ListMarketListingsAsync(page, pageSize);
            return Ok(listings);
        }

        [HttpGet("transactions-listing")]
        public async Task<IActionResult> GetTransactions([FromQuery] TransactionDateRangeRequest request)
        {
            try
            {
                var (transactions, totalCount) = await _dashboardService
                    .GetTransactionsByDateRangeAsync(request);

                return Ok(new
                {
                    Transactions = transactions,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("total-amount")]
        public async Task<ActionResult<TotalTransactionRequest>> GetTotalTransactionAmount()
        {
            var total = await _dashboardService.GetTotalTransactionAmountAsync();
            return Ok(total);
        }

        [HttpGet("transactions/count")]
        [ProducesResponseType(typeof(TransactionCountResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TransactionCountResponse>> GetTotalTransactionCount()
        {
            try
            {
                var response = await _dashboardService.GetTotalTransactionCountAsync();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while processing your request." });
            }
        }
    }
}
