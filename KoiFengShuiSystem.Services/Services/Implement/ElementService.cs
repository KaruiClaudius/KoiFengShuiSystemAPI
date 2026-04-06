using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class ElementService : IElementService
    {
        private readonly UnitOfWorkRepository _unitOfWork;
        public ElementService(UnitOfWorkRepository unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IBusinessResult> GetAllElement()
        {
            try
            {
                var result = await _unitOfWork.ElementRepository.GetAllAsync();

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
