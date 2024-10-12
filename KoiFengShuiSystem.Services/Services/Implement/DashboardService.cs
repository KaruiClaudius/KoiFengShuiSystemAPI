﻿using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class DashboardService : IDashboardService
    {
        private readonly GenericRepository<Account> _accountRepository;
        private readonly GenericRepository<TrafficLog> _trafficLogRepository;
        private readonly GenericRepository<MarketplaceListing> _marketplaceListingRepository;
        private readonly ILogger<TrafficLog> _logger;


        public DashboardService(
            GenericRepository<Account> accountRepository,
            GenericRepository<TrafficLog> trafficLogRepository,
            GenericRepository<MarketplaceListing> marketplaceListingRepository,
            ILogger<TrafficLog> logger)
        {
            _accountRepository = accountRepository;
            _trafficLogRepository = trafficLogRepository;
            _marketplaceListingRepository = marketplaceListingRepository;
            _logger = logger;
        }

        public async Task<int> CountNewUsersAsync(int days)
        {
            return (await ListNewUsersAsync(days)).Count;
        }

        public async Task<List<Account>> ListNewUsersAsync(int days)
        {
            if (days <= 0)
            {
                throw new ArgumentException("Days must be a positive integer.", nameof(days));
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _accountRepository.GetAllQuery().AsQueryable()
                .Where(a => a.CreateAt != null && a.CreateAt >= cutoffDate)
                .OrderByDescending(a => a.CreateAt)
                .ToListAsync();
        }

        public async Task<int> GetRegisteredUsersTrafficCount()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var query = _trafficLogRepository.GetAllQuery().AsQueryable()
                .Where(log => log.IsRegistered && log.Timestamp >= thirtyDaysAgo);

            var count = await query
                .Select(log => log.AccountId)
                .Distinct()
                .CountAsync();

            // Add some logging
            _logger.LogInformation($"Registered users traffic count: {count}");
            _logger.LogInformation($"Query: {query.ToQueryString()}");

            return count;
        }

        public async Task<int> GetUniqueGuestsTrafficCount()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            return await _trafficLogRepository.GetAllQuery().AsQueryable()
                .Where(log => !log.IsRegistered && log.Timestamp >= thirtyDaysAgo)
                .Select(log => log.IpAddress) // Use IP address for guests
                .Distinct()
                .CountAsync();
        }



        public async Task<int> CountNewMarketListingsAsync(int days)
        {
            if (days <= 0)
            {
                throw new ArgumentException("Days must be a positive integer.", nameof(days));
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _marketplaceListingRepository.GetAllQuery()
                .CountAsync(l => l.CreateAt >= cutoffDate);
        }

        public async Task<List<CategoryListingCount>> CountNewMarketListingsByCategoryAsync(int days)
        {
            if (days <= 0)
            {
                throw new ArgumentException("Days must be a positive integer.", nameof(days));
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var query = await _marketplaceListingRepository.GetAllQuery()
                .Where(l => l.CreateAt >= cutoffDate)
                .GroupBy(l => l.Category.CategoryName)
                .Select(g => new CategoryListingCount
                {
                    CategoryName = g.Key,
                    Count = g.Count(),
                    CategoryOutput = $"{g.Key} (Count: {g.Count()})"
                })
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return query;
        }

        public async Task<List<MarketListingSummary>> ListMarketListingsAsync(int page = 1, int pageSize = 10)
        {
            return await _marketplaceListingRepository.GetAllQuery()
                .OrderByDescending(l => l.CreateAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new MarketListingSummary
                {
                    ListingId = l.ListingId,
                    Title = l.Title,
                    IsActive = l.IsActive
                })
                .ToListAsync();
        }
    }


}

