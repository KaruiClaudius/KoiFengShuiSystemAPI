using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Interface; 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _transactionRepository.AddAsync(transaction);
        }

        public async Task<List<Transaction>> GetTransactionsByAccountIdAsync(int accountId)
        {
            return await _transactionRepository.GetByAccountIdAsync(accountId);
        }

        public async Task<List<Transaction>> GetTransactionsByAccountIdAndDateAsync(int accountId, DateTime date)
        {
            return await _transactionRepository.GetByAccountIdAndDateAsync(accountId, date);
        }

        public async Task<decimal> GetTotalAmountByAccountIdAsync(int accountId)
        {
            return await _transactionRepository.GetTotalAmountByAccountIdAsync(accountId);
        }

        public async Task DeleteByAccountIdAsync(int accountId)
        {
            await _transactionRepository.DeleteByAccountIdAsync(accountId);
        }

        public async Task<Transaction> GetTransactionByIdAsync(int transactionId)
        {
            return await _transactionRepository.GetByIdAsync(transactionId);
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            return await _transactionRepository.ValidateAsync(transaction);
        }
    }
}