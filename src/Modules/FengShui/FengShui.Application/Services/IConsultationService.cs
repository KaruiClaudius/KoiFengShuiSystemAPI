using KoiFengShuiSystem.Modules.FengShui.Application.Responses;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Services
{
    public interface IConsultationService
    {
        Task<FengShuiResponse> GetFengShuiConsultationAsync(int yearOfBirth, bool isMale);
    }
}
