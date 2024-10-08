using KoiFengShuiSystem.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface ITransactionService
    {
        Task AddAsync(Transaction transaction);
        Task<List<Transaction>> GetTransactionsByAccountIdAsync(int accountId);
        Task<List<Transaction>> GetTransactionsByAccountIdAndDateAsync(int accountId, DateTime date);
        Task<decimal> GetTotalAmountByAccountIdAsync(int accountId);
        Task DeleteByAccountIdAsync(int accountId);
        Task<Transaction> GetTransactionByIdAsync(int transactionId);
        Task<bool> ValidateTransactionAsync(Transaction transaction);
    }
}