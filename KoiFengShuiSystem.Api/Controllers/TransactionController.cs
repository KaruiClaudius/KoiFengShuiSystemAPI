using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;

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
        public async Task<IActionResult> ProcessTransaction(int accountId, int tierId, int subscriptionId, decimal amount)
        {
            var transaction = new Transaction
            {
                AccountId = accountId,
                TierId = tierId,
                SubscriptionId = subscriptionId,
                Amount = amount,
                TransactionDate = DateTime.UtcNow
            };

            await _transactionService.AddAsync(transaction);
            return Ok(new { message = "Transaction successful" });
        }

        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetAllTransactions(int accountId)
        {
            var transactions = await _transactionService.GetAllByAccountIdAsync(accountId);
            return Ok(transactions);
        }

        [HttpGet("date/{accountId}/{date}")]
        public async Task<IActionResult> GetTransactionsByDate(int accountId, DateTime date)
        {
            var transactions = await _transactionService.GetAllByAccountIdAndDateAsync(accountId, date);
            return Ok(transactions);
        }

        [HttpGet("total/{accountId}")]
        public async Task<IActionResult> GetTotalAmount(int accountId)
        {
            var totalAmount = await _transactionService.GetTotalAmountByAccountIdAsync(accountId);
            return Ok(new { totalAmount });
        }

        [HttpDelete("delete/{accountId}")]
        public async Task<IActionResult> DeleteTransactions(int accountId)
        {
            await _transactionService.DeleteByAccountIdAsync(accountId);
            return Ok(new { message = "All transactions deleted." });
        }
    }
}