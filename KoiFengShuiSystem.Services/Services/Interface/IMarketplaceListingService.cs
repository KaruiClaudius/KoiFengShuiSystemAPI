using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.AspNetCore.Http;
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
        Task<IBusinessResult> GetMarketplaceListingByElementId(int elementId, int categoryId, int excludeListingId, int pageNumber, int pageSize);
        Task<IBusinessResult> GetMarketplaceListingByAccountId(int accountId, int categoryId, int excludeListingId, int pageNumber, int pageSize);
        Task<IBusinessResult> CreateMarketplaceListing(MarketplaceListingRequest marketplaceListing, List<IFormFile> imgFiles, int userId);
        // Helper method to compare two payments
        Task<IBusinessResult> DeleteMarketplaceListing(int id);
        Task<IBusinessResult> Save();
    }
}
