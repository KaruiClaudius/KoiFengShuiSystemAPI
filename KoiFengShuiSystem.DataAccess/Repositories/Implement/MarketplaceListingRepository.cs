using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class MarketplaceListingRepository : GenericRepository<MarketplaceListing>
    {
        public MarketplaceListingRepository() { }
        public async Task<IEnumerable<MarketplaceListing>> GetAllWithElementAsync()
        {
            return await _dbSet
                .Include(p => p.Element) // Include the Element to access ElementName
                .Include(p => p.Account)
                .Include(p => p.Tier)
                .OrderByDescending(p => p.Tier.TierName == "Preminum") // Premium listings first
                .ThenByDescending(p => p.CreateAt)
                .ToListAsync();

        }
        public async Task<GenericRepository<MarketplaceListing>> GetAllByCategoryTypeIdAsync(int categoryId, int pageNumber, int pageSize)
        {
            var marketplaces = await _dbSet
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Element) // Include the Element to access ElementName
                .Include(p => p.Account)
                .Include(p => p.Tier)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .OrderByDescending(p => p.Tier.TierName == "Preminum") // Premium listings first
                .ThenByDescending(p => p.CreateAt)
                .ToListAsync();
            var totalCount = await _dbSet.CountAsync(p => p.CategoryId == categoryId);

            return new GenericRepository<MarketplaceListing>(marketplaces, totalCount);
        }
    }
}
