using KoiFengShuiSystem.DataAccess.Models;
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
        Task<int> GetGuestsTrafficCount();
    }
}
