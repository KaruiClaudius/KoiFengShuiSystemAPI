using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Modules.FengShui.Api.Controllers
{
    [ApiController]
    [Route("api/partner-shops")]
    public class PartnerShopsController : Controller
    {
        // RoleId minted into the JWT by Identity's JwtTokenService (ClaimTypes.Role = account.RoleId); 1 = admin.
        private const string AdminRole = "1";

        private readonly IPartnerShopService _partnerShopService;

        public PartnerShopsController(IPartnerShopService partnerShopService)
        {
            _partnerShopService = partnerShopService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            var shops = await _partnerShopService.GetActiveAsync();
            return Ok(shops);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var shop = await _partnerShopService.GetByIdAsync(id);
                return Ok(shop);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [Authorize(Roles = AdminRole)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PartnerShopRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var created = await _partnerShopService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = AdminRole)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PartnerShopRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _partnerShopService.UpdateAsync(id, request);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [Authorize(Roles = AdminRole)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _partnerShopService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
