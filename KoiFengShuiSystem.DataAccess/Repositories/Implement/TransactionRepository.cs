using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;

namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly KoiFengShuiContext _context;

        public TransactionRepository(KoiFengShuiContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Transaction>> GetByAccountIdAsync(int accountId)
        {
            return await _context.Transactions.Where(t => t.AccountId == accountId).ToListAsync();
        }

        public async Task<List<Transaction>> GetByAccountIdAndDateAsync(int accountId, DateTime date)
        {
            return await _context.Transactions
                .Where(t => t.AccountId == accountId && t.TransactionDate.Date == date.Date)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalAmountByAccountIdAsync(int accountId)
        {
            return await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .SumAsync(t => t.Amount);
        }

        public async Task DeleteByAccountIdAsync(int accountId)
        {
            var transactions = await _context.Transactions.Where(t => t.AccountId == accountId).ToListAsync();
            _context.Transactions.RemoveRange(transactions);
            await _context.SaveChangesAsync();
        }

        public async Task<Transaction> GetByIdAsync(int transactionId)
        {
            return await _context.Transactions.FindAsync(transactionId);
        }

        public async Task<bool> ValidateAsync(Transaction transaction)
        {
            // Logic kiểm tra tính hợp lệ
            return true; // Hoặc false nếu không hợp lệ
        }
    }
}
