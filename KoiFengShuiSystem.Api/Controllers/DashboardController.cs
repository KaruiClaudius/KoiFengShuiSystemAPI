using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.API.Controllers
{
    [ApiController]
    [Authorize(Roles = AuthorizationDefaults.Roles.Admin)]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private const string GenericErrorMessage = "An error occurred while processing your request.";
        private const string InvalidDaysMessage = "Days must be a positive integer.";

        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet("new-users-count")]
        public async Task<IActionResult> GetNewUsersCount([FromQuery] int days = 30)
        {
            try
            {
                var count = await _dashboardService.CountNewUsersAsync(days);
                return Ok(new { Count = count });
            }
            catch (ArgumentException)
            {
                return BadRequest(InvalidDaysMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard new-users-count failed for days={Days}", days);
                return StatusCode(500, GenericErrorMessage);
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
            catch (ArgumentException)
            {
                return BadRequest(InvalidDaysMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard new-users-list failed for days={Days}", days);
                return StatusCode(500, GenericErrorMessage);
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
                if (total == 0)
                {
                    return Ok(new
                    {
                        RegisteredUsers = 0d,
                        UniqueGuests = 0d,
                        TotalVisitors = 0
                    });
                }

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
                _logger.LogError(ex, "Dashboard traffic-distribution failed");
                return StatusCode(500, GenericErrorMessage);
            }
        }
    }
}
