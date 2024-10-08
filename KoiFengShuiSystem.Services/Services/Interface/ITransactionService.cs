using KoiFengShuiSystem.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static KoiFengShuiSystem.Shared.Models.Response.TransactionResponseDto;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface ITransactionService
    {
        Task<Transaction> ProcessTransactionAsync(TransactionRequestDto transactionRequest);
        Task<List<Transaction>> GetByAccountIdAsync(int accountId);
        Task DeleteTransactionAsync(int transactionId);
        Task<decimal> GetTotalAmountByAccountIdAsync(int accountId);
    }
}