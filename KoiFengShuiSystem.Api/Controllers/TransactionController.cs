using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static KoiFengShuiSystem.Shared.Models.Response.TransactionResponseDto;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IVnPayService _vnPayService;

        public TransactionController(ITransactionService transactionService, IVnPayService vnPayService)
        {
            _transactionService = transactionService;
            _vnPayService = vnPayService;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> ProcessTransaction([FromBody] TransactionRequestDto transactionRequest)
        {
            var transaction = await _transactionService.ProcessTransactionAsync(transactionRequest);
            var response = new
            {
                AccountId = transaction.AccountId,
                TierId = transaction.TierId,
                SubscriptionId = transaction.SubscriptionId,
                Amount = transaction.Amount,
                TransactionDate = transaction.TransactionDate,
                Status = transaction.Status,
                ListingId = transaction.ListingId
            };

            return Ok(response);
        }

        [HttpPost("CreatePayment")]
        public IActionResult CreatePayment([FromBody] VnPaymentRequestModel paymentRequest)
        {
            var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, paymentRequest);
            return Ok(new { PaymentUrl = paymentUrl });
        }

        [HttpPost("PaymentExecute")]
        public IActionResult PaymentExecute(IQueryCollection collections)
        {
            var paymentResponse = _vnPayService.PaymentExecute(collections);
            if (paymentResponse.Success)
            {     
                return Ok(paymentResponse);
            }
            return BadRequest("Thanh toán không thành công.");
        }
    }
}