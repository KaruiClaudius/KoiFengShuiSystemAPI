using KoiFengShuiSystem.Api.Authorization;
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
        [HttpGet("ByAccount/{accountId}")]
        public async Task<IActionResult> GetAllTransactionsByAccountId(int accountId)
        {
            var transactions = await _transactionService.GetByAccountIdAsync(accountId);
            return Ok(transactions);
        }


        [HttpGet("TotalAmount/{accountId}")]
        public async Task<IActionResult> GetTotalAmountByAccountId(int accountId)
        {
            var totalAmount = await _transactionService.GetTotalAmountByAccountIdAsync(accountId);
            return Ok(new { TotalAmount = totalAmount });
        }

        [HttpDelete("DeleteAll/{accountId}")]
        public async Task<IActionResult> DeleteAllTransactionsByAccountId(int accountId)
        {
            await _transactionService.DeleteAllTransactionsByAccountIdAsync(accountId);
            return NoContent();
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

    //    [HttpPost("CreatePayment")]
    //public IActionResult CreatePayment([FromBody] VnPaymentRequestModel paymentRequest)
    //{
    //    var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, paymentRequest);
    //    return Ok(new { PaymentUrl = paymentUrl });
    //}

        // Endpoint để xử lý phản hồi từ VnPay
        //public async Task<IActionResult> PaymentExecute(IQueryCollection collections)
        //{
        //    var paymentResponse = _vnPayService.PaymentExecute(collections);
        //    if (paymentResponse.Success)
        //    {
        //        // Tạo giao dịch trong cơ sở dữ liệu sau khi thanh toán thành công
        //        var transaction = new Transaction
        //        {
        //            AccountId = /* ID tài khoản */,
        //            TierId = /* ID cấp độ */,
        //            SubscriptionId = /* ID đăng ký */,
        //            Amount = /* Số tiền */, // Sử dụng trường Amount
        //            TransactionDate = DateTime.Now, // Ngày giao dịch
        //            Status = "Success", // Trạng thái giao dịch
        //            ListingId = /* ID danh sách */,
        //        };

        //        await _transactionService.ProcessTransactionAsync(transaction); // Lưu giao dịch vào cơ sở dữ liệu
        //        return Ok(paymentResponse);
        //    }
        //    return BadRequest("Thanh toán không thành công.");
        //}
        //[HttpPost("Create")]
        //public async Task<IActionResult> ProcessTransaction([FromBody] TransactionRequestDto transactionRequest)
        //{
        //    var transaction = await _transactionService.ProcessTransactionAsync(transactionRequest);
        //    var response = new
        //    {
        //        AccountId = transaction.AccountId,
        //        TierId = transaction.TierId,
        //        SubscriptionId = transaction.SubscriptionId,
        //        Amount = transaction.Amount,
        //        TransactionDate = transaction.TransactionDate,
        //        Status = transaction.Status,
        //        ListingId = transaction.ListingId
        //    };

        //    return Ok(response);
        //}

        //public async Task<IActionResult> PaymentExecute(IQueryCollection collections,TransactionRequestDto transactionRequest)
        //{
        //    var paymentResponse = _vnPayService.PaymentExecute(collections);
        //     var transaction = await _transactionService.ProcessTransactionAsync(transactionRequest);
        //    if (paymentResponse.Success)
        //    {
        //        // Tạo giao dịch trong cơ sở dữ liệu sau khi thanh toán thành công
        //        var transaction = new Transaction
        //        {
        //            AccountId = /* ID tài khoản */,
        //            TierId = /* ID cấp độ */,
        //            SubscriptionId = /* ID đăng ký */,
        //            Amount = /* Số tiền */, // Sử dụng trường Amount
        //            TransactionDate = DateTime.Now, // Ngày giao dịch
        //            Status = "Success", // Trạng thái giao dịch
        //            ListingId = /* ID danh sách */,
        //        };

        //        await _transactionService.ProcessTransactionAsync(transaction); // Lưu giao dịch vào cơ sở dữ liệu
        //        return Ok(paymentResponse);
        //    }
        //    return BadRequest("Thanh toán không thành công.");
        //}
    }
}