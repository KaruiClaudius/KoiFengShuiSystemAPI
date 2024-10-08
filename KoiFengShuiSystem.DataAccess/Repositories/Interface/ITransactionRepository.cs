using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KoiFengShuiSystem.DataAccess.Models;

namespace KoiFengShuiSystem.DataAccess.Repositories.Interface
{
    public interface ITransactionRepository
    {
        Task AddAsync(Transaction transaction);
        Task<List<Transaction>> GetByAccountIdAsync(int accountId);
        Task<List<Transaction>> GetByAccountIdAndDateAsync(int accountId, DateTime date);
        Task<decimal> GetTotalAmountByAccountIdAsync(int accountId);
        Task DeleteByAccountIdAsync(int accountId);
        Task<Transaction> GetByIdAsync(int transactionId);
        Task<bool> ValidateAsync(Transaction transaction);
    }
}