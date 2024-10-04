using KoiFengShuiSystem.BusinessLogic.Services.Interface;
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
                var guests = await _dashboardService.GetGuestsTrafficCount();

                return Ok(new { registeredUsers, guests });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

    }
}
