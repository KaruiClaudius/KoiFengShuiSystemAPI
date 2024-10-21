using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface ITransactionService
    {
        Task<MessageResponse> CreatePaymentLink(CreatePaymentLinkRequest body, int listingId, int tierId, string userEmail);
        //Task<MessageResponse> PaymentCallback(PaymentCallbackData callbackData);
        Task<MessageResponse> GetOrder(int orderCode);
        Task<MessageResponse> CancelOrder(int orderCode, string reason);
        Task<MessageResponse> ConfirmWebhook(ConfirmWebhook body);
        Task<MessageResponse> CheckOrder(CheckOrderRequest request, string userEmail);
    }
}
