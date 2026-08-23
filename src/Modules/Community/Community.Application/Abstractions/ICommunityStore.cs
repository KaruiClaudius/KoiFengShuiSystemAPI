using KoiFengShuiSystem.DataAccess.Models;

namespace KoiFengShuiSystem.Modules.Community.Application.Abstractions
{
    public interface ICommunityStore
    {
        Task<IReadOnlyList<FAQ>> GetAllAsync();

        Task<FAQ?> GetByIdAsync(int id);

        Task<FAQ> AddAsync(FAQ faq);

        Task UpdateAsync(FAQ faq);

        Task<bool> DeleteAsync(int id);
    }
}
