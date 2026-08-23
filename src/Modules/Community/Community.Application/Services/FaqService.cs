using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Responses;

namespace KoiFengShuiSystem.Modules.Community.Application.Services
{
    public class FaqService : IFaqService
    {
        private readonly ICommunityStore _store;

        public FaqService(ICommunityStore store)
        {
            ArgumentNullException.ThrowIfNull(store);
            _store = store;
        }

        public async Task<IReadOnlyList<FAQResponse>> GetAllFAQsAsync()
        {
            var faqs = await _store.GetAllAsync();
            return faqs.Select(ToResponse).ToList();
        }

        public async Task<FAQResponse?> GetFAQByIdAsync(int id)
        {
            var faq = await _store.GetByIdAsync(id);
            return faq == null ? null : ToResponse(faq);
        }

        public async Task<FAQResponse> CreateFAQAsync(FAQRequest faqRequest)
        {
            ArgumentNullException.ThrowIfNull(faqRequest);

            var faq = new FAQ
            {
                Question = faqRequest.Question,
                Answer = faqRequest.Answer,
                CreateAt = DateTime.Now,
                AccountId = faqRequest.AccountId
            };

            var created = await _store.AddAsync(faq);
            return ToResponse(created);
        }

        public async Task<FAQResponse?> UpdateFAQAsync(int id, FAQRequest faqRequest)
        {
            ArgumentNullException.ThrowIfNull(faqRequest);

            var faq = await _store.GetByIdAsync(id);
            if (faq == null)
            {
                return null;
            }

            faq.Question = faqRequest.Question;
            faq.Answer = faqRequest.Answer;
            faq.CreateAt = DateTime.Now;

            await _store.UpdateAsync(faq);
            return ToResponse(faq);
        }

        public Task<bool> DeleteFAQAsync(int id)
            => _store.DeleteAsync(id);

        private static FAQResponse ToResponse(FAQ faq) => new()
        {
            FAQId = faq.FAQId,
            Question = faq.Question,
            Answer = faq.Answer,
            CreateAt = faq.CreateAt
        };
    }
}
