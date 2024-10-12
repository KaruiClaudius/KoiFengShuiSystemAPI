using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
   public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContent content, VnPaymentResponseModel model);
        VnPaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
