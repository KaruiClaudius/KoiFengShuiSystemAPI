using KoiFengShuiSystem.Shared.Kernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IElementService
    {
        Task<IBusinessResult> GetAllElement();
    }
}
