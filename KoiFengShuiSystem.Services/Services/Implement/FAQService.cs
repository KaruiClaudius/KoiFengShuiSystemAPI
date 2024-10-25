using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class FAQService : IFAQService
    {
        private readonly KoiFengShuiContext _context;

        public FAQService(KoiFengShuiContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FAQResponse>> GetAllFAQsAsync()
        {
            var faqs = await _context.FAQs.ToListAsync();
            return faqs.Select(f => new FAQResponse
            {
                FAQId = f.FAQId,
                Question = f.Question,
                Answer = f.Answer,
                CreateAt = f.CreateAt
            });
        }

        public async Task<FAQResponse> GetFAQByIdAsync(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq == null) return null;

            return new FAQResponse
            {
                FAQId = faq.FAQId,
                Question = faq.Question,
                Answer = faq.Answer,
                CreateAt = faq.CreateAt
            };
        }

        public async Task<FAQResponse> CreateFAQAsync(FAQRequest faqRequest)
        {
            var faq = new FAQ
            {
                Question = faqRequest.Question,
                Answer = faqRequest.Answer,
                CreateAt = DateTime.Now,
                AccountId = faqRequest.AccountId
            };

            _context.FAQs.Add(faq);
            await _context.SaveChangesAsync();

            return new FAQResponse
            {
                FAQId = faq.FAQId,
                Question = faq.Question,
                Answer = faq.Answer,
                CreateAt = faq.CreateAt
            };
        }

        public async Task<FAQResponse> UpdateFAQAsync(int id, FAQRequest faqRequest)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq == null) return null;

            faq.Question = faqRequest.Question;
            faq.Answer = faqRequest.Answer;
            faq.CreateAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return new FAQResponse
            {
                FAQId = faq.FAQId,
                Question = faq.Question,
                Answer = faq.Answer,
                CreateAt = faq.CreateAt
            };
        }

        public async Task<bool> DeleteFAQAsync(int id)
        {
            var faq = await _context.FAQs.FindAsync(id);
            if (faq == null) return false;

            _context.FAQs.Remove(faq);
            await _context.SaveChangesAsync();

            return true;
        }

    }
}   