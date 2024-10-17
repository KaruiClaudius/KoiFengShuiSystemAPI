using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static KoiFengShuiSystem.Shared.Models.Response.TransactionResponseDto;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(ITransactionService transactionService, ILogger<TransactionController> logger )
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> ProcessTransaction([FromBody] TransactionRequestDto transactionRequest)
        {
            var transaction = await _transactionService.ProcessTransactionAsync(transactionRequest);
            var response = new
            {
                AccountId = transaction.AccountId,
                TierId = transaction.TierId,
                SubscriptionId = transaction.SubscriptionId,
                Amount = transaction.Amount,
                TransactionDate = transaction.TransactionDate,
                Status = transaction.Status,
                ListingId = transaction.ListingId
            };

            return Ok(response);
        }
        [HttpGet("ByAccount/{accountId}")]
        public async Task<IActionResult> GetAllTransactionsByAccountId(int accountId)
        {
            var transactions = await _transactionService.GetByAccountIdAsync(accountId);
            return Ok(transactions);
        }


        [HttpGet("TotalAmount/{accountId}")]
        public async Task<IActionResult> GetTotalAmountByAccountId(int accountId)
        {
            var totalAmount = await _transactionService.GetTotalAmountByAccountIdAsync(accountId);
            return Ok(new { TotalAmount = totalAmount });
        }

        [HttpDelete("DeleteAll/{accountId}")]
        public async Task<IActionResult> DeleteAllTransactionsByAccountId(int accountId)
        {
            await _transactionService.DeleteAllTransactionsByAccountIdAsync(accountId);
            return NoContent();
        }
    }
}