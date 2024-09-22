using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate(AuthenticateRequest model)
        {
            var response = _accountService.Authenticate(model);

            if (response == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            return Ok(response);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _accountService.GetAll();
            return Ok(users);
        }
        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var account = _accountService.GetById(id);
            return account == null ? NotFound() : Ok(account);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register(RegisterRequest model)
        {
            try
            {
                var account = _accountService.Register(model);
                return Ok(account);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateRequest model)
        {
            try
            {
                _accountService.Update(id, model);
                return Ok(new { message = "Account updated successfully" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                _accountService.Delete(id);
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("logout")]
        public IActionResult Logout()
        {
            // Clear the user's session data
            HttpContext.Session.Clear();

            // Return a success response
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
