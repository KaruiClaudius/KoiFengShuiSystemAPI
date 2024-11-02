using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IDashboardService
    {
        Task<int> CountNewUsersAsync(int days);
        Task<List<Account>> ListNewUsersAsync(int days);
        Task<int> GetRegisteredUsersTrafficCount();
        Task<int> GetUniqueGuestsTrafficCount();
        Task<int> CountNewMarketListingsAsync(int days);
        Task<List<CategoryListingCount>> CountNewMarketListingsByCategoryAsync(int days);
        Task<List<MarketListingSummary>> ListMarketListingsAsync(int page = 1, int pageSize = 10);
        Task<(IEnumerable<TransactionDashboardRequest> Transactions, int TotalCount)>
         GetTransactionsByDateRangeAsync(TransactionDateRangeRequest request);
        Task<TotalTransactionRequest> GetTotalTransactionAmountAsync();
        Task<TransactionCountResponse> GetTotalTransactionCountAsync();

    }
}
