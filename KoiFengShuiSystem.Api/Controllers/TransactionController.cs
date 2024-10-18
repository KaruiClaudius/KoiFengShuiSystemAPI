using Azure;
using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static KoiFengShuiSystem.Shared.Models.Response.TransactionResponseDto;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly GenericRepository<Account> _accountRepository;
        private readonly GenericRepository<DataAccess.Models.Transaction> _transactionRepository;
        private readonly PayOS _payOS;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TransactionController(
            GenericRepository<Account> accountRepository,
            GenericRepository<DataAccess.Models.Transaction> transactionRepository,
            PayOS payOS,
            IHttpContextAccessor httpContextAccessor)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _payOS = payOS;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("CreatePayOSLink")]
        public async Task<IActionResult> CreatePaymentLink(CreatePaymentLinkRequest body)
        {
            try
            {
                var userEmail = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);


                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User not authenticated");
                }

                var user = await _accountRepository.FindAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                int orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));

                ItemData item = new ItemData(body.productName, 1, body.price);
                List<ItemData> items = new List<ItemData> { item };

                string buyerName = !string.IsNullOrEmpty(body.buyerName) ? body.buyerName : user.FullName;

                PaymentData paymentData = new PaymentData(
                    orderCode,
                    body.price,
                    body.description,
                    items,
                    body.cancelUrl,
                    body.returnUrl,
                    buyerName
                );

                CreatePaymentResult createPayment = await _payOS.createPaymentLink(paymentData);

                //// Save the payment information to the database
                //var paymentTransaction = new DataAccess.Models.Transaction
                //{
                //    TransactionId = orderCode,
                //    AccountId = user.AccountId,
                //    Amount = body.price,
                //    Status = "PENDING",
                //    TransactionDate = DateTime.UtcNow,

                //};

                //await _transactionRepository.CreateAsync(paymentTransaction);

                // Prepare response with current user info
                var currentUserInfo = new
                {
                    user.AccountId,
                    user.FullName,
                    user.Email,
                    user.Phone
                };

                return Ok(new MessageResponse(0, "success", new
                {
                    paymentInfo = createPayment,
                    userInfo = currentUserInfo
                }));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return Ok(new MessageResponse(-1, "fail", null));
            }
        }

        [HttpGet("getPayOSOrder/{orderCode}")]
        public async Task<IActionResult> GetOrder([FromRoute] int orderCode)
        {
            try
            {
                PaymentLinkInformation paymentLinkInformation = await _payOS.getPaymentLinkInformation(orderCode);
                return Ok(new MessageResponse(0, "Ok", paymentLinkInformation));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return Ok(new MessageResponse(-1, "fail", null));
            }
        }

        [HttpPut("cancelOrder/{orderCode}")]
        public async Task<IActionResult> CancelOrder([FromRoute] int orderCode, string reason)
        {
            try
            {
                PaymentLinkInformation paymentLinkInformation = await _payOS.cancelPaymentLink(orderCode, reason);
                return Ok(new MessageResponse(0, "Ok", paymentLinkInformation));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return Ok(new MessageResponse(-1, "fail", null));
            }
        }

        [HttpPost("confirm-webhook")]
        public async Task<IActionResult> ConfirmWebhook(ConfirmWebhook body)
        {
            try
            {
                await _payOS.confirmWebhook(body.webhook_url);
                return Ok(new MessageResponse(0, "Ok", null));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return Ok(new MessageResponse(-1, "fail", null));
            }
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
