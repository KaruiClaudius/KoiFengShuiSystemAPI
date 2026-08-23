using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Shared.Kernel.Results;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Services
{
    public class ElementService : IElementService
    {
        private readonly IFengShuiReadStore _readStore;

        public ElementService(IFengShuiReadStore readStore)
        {
            _readStore = readStore;
        }

        public async Task<IBusinessResult> GetAllElement()
        {
            try
            {
                var result = await _readStore.GetAllElementsAsync();

                if (result != null)
                {
                    return new BusinessResult(1, "Get data success", result);
                }
                else
                {
                    return new BusinessResult(-1, "Get data fail");
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(-4, ex.ToString());
            }
        }
    }
}
