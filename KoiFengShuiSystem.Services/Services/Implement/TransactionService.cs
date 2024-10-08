using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Interface; 
using System;
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
                TransactionDate = transactionRequest.TransactionDate
            };

            await _transactionRepository.AddAsync(transaction);
            return transaction;
        }
    }
}