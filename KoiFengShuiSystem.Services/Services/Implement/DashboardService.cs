using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
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

        public DashboardService(
            GenericRepository<Account> accountRepository,
            GenericRepository<TrafficLog> trafficLogRepository)
        {
            _accountRepository = accountRepository;
            _trafficLogRepository = trafficLogRepository;
        }

        public async Task<int> CountNewUsersAsync(int days)
        {
            if (days <= 0)
            {
                throw new ArgumentException("Days must be a positive integer.", nameof(days));
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _accountRepository.GetAllQuery().AsQueryable()
                .CountAsync(a => a.CreateAt.HasValue && a.CreateAt.Value >= cutoffDate);
        }

        public async Task<List<Account>> ListNewUsersAsync(int days)
        {
            if (days <= 0)
            {
                throw new ArgumentException("Days must be a positive integer.", nameof(days));
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _accountRepository.GetAllQuery().AsQueryable()
                .Where(a => a.CreateAt.HasValue && a.CreateAt.Value >= cutoffDate)
                .ToListAsync();
        }

        public async Task<int> GetRegisteredUsersTrafficCount()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            return await _trafficLogRepository.GetAllQuery().AsQueryable()
                .CountAsync(log => log.IsRegistered && log.Timestamp >= thirtyDaysAgo);
        }

        public async Task<int> GetGuestsTrafficCount()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            return await _trafficLogRepository.GetAllQuery().AsQueryable()
                .CountAsync(log => !log.IsRegistered && log.Timestamp >= thirtyDaysAgo);
        }


    }
}
