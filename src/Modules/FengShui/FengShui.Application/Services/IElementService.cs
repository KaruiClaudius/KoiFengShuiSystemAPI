using KoiFengShuiSystem.Shared.Kernel.Results;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Services
{
    public interface IElementService
    {
        Task<IBusinessResult> GetAllElement();
    }
}
