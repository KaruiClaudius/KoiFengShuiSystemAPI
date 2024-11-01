using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class SubcriptionTiersService : ISubcriptionTiersService
    {
        private readonly UnitOfWorkRepository _unitOfWork;
        public SubcriptionTiersService()
        {
            _unitOfWork = new UnitOfWorkRepository();
        }
        public async Task<IBusinessResult> GetAllSubcriptionTiers()
        {
            try
            {
                var result = await _unitOfWork.SubcriptionTiersRepository.GetAllAsync();

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
