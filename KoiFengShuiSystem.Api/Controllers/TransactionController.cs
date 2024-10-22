using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        [HttpPost("Create")]
        public async Task<IActionResult> ProcessTransaction([FromBody] TransactionRequestDto transactionRequest)
        {
            var transaction = await _transactionService.ProcessTransactionAsync(transactionRequest);
            var response = new
            {
                TransactionId = transaction.TransactionId,
                AccountId = transaction.AccountId,
                TierId = transaction.TierId,
                
                Amount = transaction.Amount,
                TransactionDate = transaction.TransactionDate
            };

            return Ok(response);
        }


        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetTransactionsByAccountId(int accountId)
        {
            var transactions = await _transactionService.GetByAccountIdAsync(accountId);
            return Ok(transactions);
        }

        [HttpDelete("{transactionId}")]
        public async Task<IActionResult> DeleteTransaction(int transactionId)
        {
            await _transactionService.DeleteTransactionAsync(transactionId);
            return NoContent(); // Trả về 204 No Content
        }

        [HttpGet("total/{accountId}")]
        public async Task<IActionResult> GetTotalAmountByAccountId(int accountId)
        {
            var totalAmount = await _transactionService.GetTotalAmountByAccountIdAsync(accountId);
            return Ok(totalAmount);
        }
    }
}