using AutoMapper.Internal;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IFAQService
    {
        Task<IEnumerable<FAQResponse>> GetAllFAQsAsync();
        Task<FAQResponse> GetFAQByIdAsync(int id);
        Task<FAQResponse> CreateFAQAsync(FAQRequest faqRequest);
        Task<FAQResponse> UpdateFAQAsync(int id, FAQRequest faqRequest);
        Task<bool> DeleteFAQAsync(int id);

    }
}