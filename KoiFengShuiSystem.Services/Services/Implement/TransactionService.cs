using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using System.Collections.Generic;
using System.Threading.Tasks;
using static KoiFengShuiSystem.Shared.Models.Response.TransactionResponseDto;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<Transaction> ProcessTransactionAsync(TransactionRequestDto transactionRequest)
        {
            var transaction = new Transaction
            {
                AccountId = transactionRequest.AccountId,
                TierId = transactionRequest.TierId,
                SubscriptionId = transactionRequest.SubscriptionId,
                Amount = transactionRequest.Amount,
                TransactionDate = transactionRequest.TransactionDate,
                Status = transactionRequest.Status,
                ListingId = transactionRequest.ListingId
            };

            await _transactionRepository.AddAsync(transaction);
            return transaction;
        }

        public async Task<List<Transaction>> GetByAccountIdAsync(int accountId)
        {
            return await _transactionRepository.GetByAccountIdAsync(accountId);
        }



      
        public async Task<decimal> GetTotalAmountByAccountIdAsync(int accountId)
        {
            return await _transactionRepository.GetTotalAmountByAccountIdAsync(accountId);
        }

    
        public async Task DeleteAllTransactionsByAccountIdAsync(int accountId)
        {
            var transactions = await _transactionRepository.GetByAccountIdAsync(accountId);
            foreach (var transaction in transactions)
            {
                await _transactionRepository.DeleteByAccountIdAsync(transaction.TransactionId);
            }
        }
    }
}