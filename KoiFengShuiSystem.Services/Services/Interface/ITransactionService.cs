using KoiFengShuiSystem.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface ITransactionService
    {
        Task AddAsync(Transaction transaction);
        Task<List<Transaction>> GetAllByAccountIdAsync(int accountId);
        Task<List<Transaction>> GetAllByAccountIdAndDateAsync(int accountId, DateTime date);
        Task<decimal> GetTotalAmountByAccountIdAsync(int accountId);
        Task DeleteByAccountIdAsync(int accountId);
    }
}