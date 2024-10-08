using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.AspNetCore.Mvc;
using static KoiFengShuiSystem.Shared.Models.Response.TransactionResponseDto;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessTransaction([FromBody] TransactionRequestDto transactionRequest)
        {
            
            var transaction = await _transactionService.ProcessTransactionAsync(transactionRequest);

         
            var response = new
            {
                TransactionId = transaction.TransactionId,
                AccountId = transaction.AccountId, 
                TierId = transaction.TierId,
                SubscriptionId = transaction.SubscriptionId,
                Amount = transaction.Amount,
                TransactionDate = transaction.TransactionDate
            };

            return Ok(response);
        }
    }
}