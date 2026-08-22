using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountEntity = KoiFengShuiSystem.Modules.Identity.Domain.Entities.Account;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class DashboardService : IDashboardService
    {
        private readonly GenericRepository<AccountEntity> _accountRepository;
        private readonly GenericRepository<TrafficLog> _trafficLogRepository;


        private readonly ILogger<TrafficLog> _logger;


        public DashboardService(
            GenericRepository<AccountEntity> accountRepository,
            GenericRepository<TrafficLog> trafficLogRepository,
        ILogger<TrafficLog> logger)
        {
            _accountRepository = accountRepository;
            _trafficLogRepository = trafficLogRepository;
            _logger = logger;
        }

        public async Task<int> CountNewUsersAsync(int days)
        {
            return (await ListNewUsersAsync(days)).Count;
        }

        public async Task<List<AccountEntity>> ListNewUsersAsync(int days)
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
    }

}

