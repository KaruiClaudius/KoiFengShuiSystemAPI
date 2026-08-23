using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Responses;

namespace KoiFengShuiSystem.Modules.Community.Application.Services
{
    public interface IFaqService
    {
        Task<IReadOnlyList<FAQResponse>> GetAllFAQsAsync();

        Task<FAQResponse?> GetFAQByIdAsync(int id);

        Task<FAQResponse> CreateFAQAsync(FAQRequest faqRequest);

        Task<FAQResponse?> UpdateFAQAsync(int id, FAQRequest faqRequest);

        Task<bool> DeleteFAQAsync(int id);
    }
}
