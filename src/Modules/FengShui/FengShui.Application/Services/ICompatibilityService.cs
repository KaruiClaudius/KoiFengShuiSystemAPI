using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Responses;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Services
{
    public interface ICompatibilityService
    {
        Task<CompatibilityResponse> AssessCompatibility(CompatibilityRequest request);
    }
}
