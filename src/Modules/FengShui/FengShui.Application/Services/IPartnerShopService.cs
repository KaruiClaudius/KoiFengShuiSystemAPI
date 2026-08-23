using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Responses;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Services
{
    public interface IPartnerShopService
    {
        Task<IReadOnlyList<PartnerShopResponse>> GetActiveAsync();

        Task<PartnerShopResponse> GetByIdAsync(int id);

        Task<PartnerShopResponse> CreateAsync(PartnerShopRequest request);

        Task UpdateAsync(int id, PartnerShopRequest request);

        Task<bool> DeleteAsync(int id);
    }
}
