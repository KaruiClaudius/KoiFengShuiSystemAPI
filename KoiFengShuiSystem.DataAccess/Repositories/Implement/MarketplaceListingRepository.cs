﻿using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

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
                 .Include(p => p.ListingImages)
                .ThenInclude(p => p.Image)
                 .OrderByDescending(p => p.Tier.TierId.Equals(2)) // 2. Tin Nổi Bật
                .ThenByDescending(p => p.CreateAt)
                .ToListAsync();

        }
        public async Task<GenericRepository<MarketplaceListing>> GetAllByCategoryTypeIdAsync(int categoryId, int pageNumber, int pageSize)
        {
            var marketplaces = await _dbSet
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Element)
                .Include(p => p.Account)
                .Include(p => p.Tier)
                .Include(p => p.ListingImages)
                .ThenInclude(p => p.Image)
                .OrderByDescending(p => p.Tier.TierId.Equals(2))
                .ThenByDescending(p => p.CreateAt)
                .Take(pageSize)
                .ToListAsync();
            var totalCount = await _dbSet.CountAsync(p => p.CategoryId == categoryId);
            return new GenericRepository<MarketplaceListing>(marketplaces, totalCount);
        }
        public async Task<GenericRepository<MarketplaceListing>> GetAllByElementIdAsync(int elementId, int categoryId, int excludeListingId, int pageNumber, int pageSize)
        {
            var marketplaces = await _dbSet
                .Where(p => p.ElementId == elementId && p.ListingId != excludeListingId && p.CategoryId == categoryId)
                .Include(p => p.Element) // Include the Element to access ElementName
                .Include(p => p.Account)
                .Include(p => p.Tier)
                 .Include(p => p.ListingImages)
                .ThenInclude(p => p.Image)
                .Skip((pageNumber - 1) * pageSize)
                .OrderByDescending(p => p.Tier.TierId.Equals(2)) // 2. Tin Nổi Bật
                .ThenByDescending(p => p.CreateAt)
                .Take(pageSize)
                .ToListAsync();
            var totalCount = await _dbSet.CountAsync(p => p.CategoryId == categoryId);

            return new GenericRepository<MarketplaceListing>(marketplaces, totalCount);
        }
        public async Task<GenericRepository<MarketplaceListing>> GetAllByAccountIdAsync(int accountId, int categoryId, int excludeListingId, int pageNumber, int pageSize)
        {
            var marketplaces = await _dbSet
                .Where(p => p.AccountId == accountId && p.CategoryId == categoryId && p.ListingId != excludeListingId)
                .Include(p => p.Element) // Include the Element to access ElementName
                .Include(p => p.Account)
                .Include(p => p.Tier)
                .Include(p => p.ListingImages)
                .ThenInclude(p => p.Image)
                .Skip((pageNumber - 1) * pageSize)
                .OrderByDescending(p => p.Tier.TierId.Equals(2)) // Premium listings first
                .ThenByDescending(p => p.CreateAt)
                .Take(pageSize)
                .ToListAsync();
            var totalCount = await _dbSet.CountAsync(p => p.CategoryId == categoryId);

            return new GenericRepository<MarketplaceListing>(marketplaces, totalCount);
        }
        public async Task<IEnumerable<MarketplaceListing>> GetAllByCategoryByIdAsync(int id)
        {
            return await _dbSet
                .Where(p => p.ListingId == id)
                .Include(p => p.Element) // Include the Element to access ElementName
                .Include(p => p.Account)
                .Include(p => p.Tier)
                 .Include(p => p.ListingImages)
                .ThenInclude(p => p.Image)
                .ToListAsync();
        }
    }
}
