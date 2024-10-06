using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class TransactionService : ITransactionService
    {
        private readonly KoiFengShuiContext _context;

        public TransactionService(KoiFengShuiContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Transaction>> GetAllByAccountIdAsync(int accountId)
        {
            return await _context.Transactions.Where(t => t.AccountId == accountId).ToListAsync();
        }

        public async Task<List<Transaction>> GetAllByAccountIdAndDateAsync(int accountId, DateTime date)
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
    }
}