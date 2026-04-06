using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class MarketCategoryService : IMarketCategoryService
    {
        private readonly UnitOfWorkRepository _unitOfWork;
        public MarketCategoryService(UnitOfWorkRepository unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IBusinessResult> GetAllMarketCategory()
        {
            try
            {
                var result = await _unitOfWork.MarketCategoryRepository.GetAllAsync();

                if (result != null)
                {
                    return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
                }
                else
                {
                    return new BusinessResult(Const.FAIL_READ_CODE, Const.FAIL_READ_MSG);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(-4, ex.ToString());
            }
        }
    }
}
