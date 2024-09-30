using KoiFengShuiSystem.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IConsultationService
    {
        Task<FengShuiResponse> GetFengShuiConsultationAsync(int yearOfBirth);
    }
}
