using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IMarketplaceListingService
    {
        Task<IBusinessResult> GetAll();
        Task<IBusinessResult> GetMarketplaceListingById(int id);
        Task<IBusinessResult> GetMarketplaceListingByCategoryId(int categoryId, int pageNumber, int pageSize);
        Task<IBusinessResult> CreateMarketplaceListing(MarketplaceListing marketplaceListing);
        // Helper method to compare two payments
        Task<IBusinessResult> DeleteMarketplaceListing(int id);
        Task<IBusinessResult> Save();
    }
}
