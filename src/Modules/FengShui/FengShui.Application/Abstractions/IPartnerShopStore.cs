using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Abstractions
{
    public interface IPartnerShopStore
    {
        Task<IReadOnlyList<PartnerShop>> GetActiveAsync();

        Task<PartnerShop?> GetByIdAsync(int id);

        Task<PartnerShop> AddAsync(PartnerShop shop);

        Task UpdateAsync(PartnerShop shop);

        Task<bool> DeleteAsync(int id);
    }
}
