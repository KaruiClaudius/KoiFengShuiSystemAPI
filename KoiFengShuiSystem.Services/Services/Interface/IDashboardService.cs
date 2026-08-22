using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using AccountEntity = KoiFengShuiSystem.Modules.Identity.Domain.Entities.Account;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IDashboardService
    {
        Task<int> CountNewUsersAsync(int days);
        Task<List<AccountEntity>> ListNewUsersAsync(int days);
        Task<int> GetRegisteredUsersTrafficCount();
        Task<int> GetUniqueGuestsTrafficCount();
    }
}
