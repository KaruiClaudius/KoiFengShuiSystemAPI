using System;
using System.Threading.Tasks;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Responses;
using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.Community.Api.Controllers
{
    /// <summary>
    /// Admin dashboard reporting, ported from the legacy API controller with
    /// byte-compatible responses for the three original endpoints plus the new
    /// content-aware summary. The legacy endpoints keep their local error handling
    /// verbatim; the new endpoint relies on the host's global exception middleware.
    /// </summary>
    [ApiController]
    [Authorize(Roles = AuthorizationDefaults.Roles.Admin)]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private const string GenericErrorMessage = "An error occurred while processing your request.";
        private const string InvalidDaysMessage = "Days must be a positive integer.";

        // Legacy traffic counters were hardcoded to a rolling 30-day window.
        private const int TrafficWindowDays = 30;

        private readonly ICommunityStore _store;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ICommunityStore store, ILogger<DashboardController> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("new-users-count")]
        public async Task<IActionResult> GetNewUsersCount([FromQuery] int days = 30)
        {
            try
            {
                if (days <= 0)
                {
                    return BadRequest(InvalidDaysMessage);
                }

                var users = await _store.GetAccountsCreatedSinceAsync(DateTime.UtcNow.AddDays(-days));
                return Ok(new { Count = users.Count });
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
                if (days <= 0)
                {
                    return BadRequest(InvalidDaysMessage);
                }

                var users = await _store.GetAccountsCreatedSinceAsync(DateTime.UtcNow.AddDays(-days));
                return Ok(users);
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
                var cutoff = DateTime.UtcNow.AddDays(-TrafficWindowDays);
                var registeredUsers = await _store.CountDistinctRegisteredTrafficSinceAsync(cutoff);
                var uniqueGuests = await _store.CountDistinctGuestTrafficSinceAsync(cutoff);

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

        /// <summary>
        /// Content-aware report: overall post volume, its distribution across
        /// categories (categories holding no posts are omitted), and the size of
        /// the pending member-submission queue.
        /// </summary>
        [HttpGet("content-summary")]
        public async Task<ActionResult<ContentSummaryResponse>> GetContentSummary()
        {
            var totalPosts = await _store.CountPostsAsync();
            var byCategory = await _store.CountPostsByCategoryAsync();
            var pendingCount = await _store.CountPendingPostsAsync();

            return Ok(new ContentSummaryResponse(totalPosts, byCategory, pendingCount));
        }
    }
}
