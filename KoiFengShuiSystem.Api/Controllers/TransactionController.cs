using Azure;
using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using System;
using System.Collections.Concurrent;
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
        //private static readonly ConcurrentDictionary<int, (int ListingId, int TierId)> _pendingTransactions = new ConcurrentDictionary<int, (int, int)>();
        //private readonly GenericRepository<Account> _accountRepository;
        //private readonly GenericRepository<MarketplaceListing> _marketplaceListingRepository;
        //private readonly GenericRepository<DataAccess.Models.Transaction> _transactionRepository;
        //private readonly PayOS _payOS;
        //private readonly IHttpContextAccessor _httpContextAccessor;
        //private readonly IUnitOfWorkRepository _unitOfWork;


        //public TransactionController(
        //    GenericRepository<Account> accountRepository,
        //    GenericRepository<MarketplaceListing> marketplaceListingRepository,
        //    GenericRepository<DataAccess.Models.Transaction> transactionRepository,
        //    PayOS payOS,
        //    IHttpContextAccessor httpContextAccessor, IUnitOfWorkRepository unitOfWork
        //    )
        //{
        //    _accountRepository = accountRepository;
        //    _transactionRepository = transactionRepository;
        //    _payOS = payOS;
        //    _httpContextAccessor = httpContextAccessor;
        //    _marketplaceListingRepository = marketplaceListingRepository;
        //    _unitOfWork = unitOfWork;
        //}

        //[HttpPost("CreatePayOSLink")]
        //public async Task<IActionResult> CreatePaymentLink(CreatePaymentLinkRequest body, [FromQuery] int listingId, [FromQuery] int tierId)
        //{
        //    try
        //    {
        //        var userEmail = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);

        //        if (string.IsNullOrEmpty(userEmail))
        //        {
        //            return Unauthorized("User not authenticated");
        //        }

        //        var user = await _accountRepository.FindAsync(u => u.Email == userEmail);

        //        if (user == null)
        //        {
        //            return NotFound("User not found");
        //        }

        //        int orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));

        //        // Store the listingId and tierId associated with this orderCode
        //        _pendingTransactions[orderCode] = (listingId, tierId);

        //        ItemData item = new ItemData(body.productName, 1, body.price);
        //        List<ItemData> items = new List<ItemData> { item };

        //        string buyerName = !string.IsNullOrEmpty(body.buyerName) ? body.buyerName : user.FullName;
        //        string buyerEmail = !string.IsNullOrEmpty(body.buyerEmail) ? body.buyerEmail : userEmail;

        //        PaymentData paymentData = new PaymentData(
        //            orderCode,
        //            body.price,
        //            body.description,
        //            items,
        //            body.cancelUrl,
        //            body.returnUrl,
        //            buyerName,
        //            buyerEmail
        //        );

        //        CreatePaymentResult createPayment = await _payOS.createPaymentLink(paymentData);

        //        // Save the payment information to the database
        //        var paymentTransaction = new DataAccess.Models.Transaction
        //        {
        //            TransactionId = orderCode,
        //            AccountId = user.AccountId,
        //            Amount = body.price,
        //            Status = "PENDING",
        //            TransactionDate = DateTime.UtcNow,
        //            ListingId = listingId,
        //            TierId = tierId
        //        };

        //        await _transactionRepository.CreateAsync(paymentTransaction);
        //        await _unitOfWork.SaveChangesWithTransactionAsync();

        //        // Prepare response with current user info
        //        var currentUserInfo = new
        //        {
        //            user.AccountId,
        //            user.FullName,
        //            user.Email,
        //            user.Phone
        //        };

        //        return Ok(new MessageResponse(0, "success", new
        //        {
        //            paymentInfo = createPayment,
        //            userInfo = currentUserInfo
        //        }));
        //    }
        //    catch (Exception exception)
        //    {
        //        Console.WriteLine(exception);
        //        return Ok(new MessageResponse(-1, "fail", null));
        //    }
        //}

        //// Add a method to handle the payment callback
        //[HttpPost("PaymentCallback")]
        //public async Task<IActionResult> PaymentCallback([FromBody] PaymentCallbackData callbackData)
        //{
        //    if (_pendingTransactions.TryRemove(callbackData.OrderCode, out var transactionInfo))
        //    {
        //        // Update the transaction in the database
        //        var transaction = await _transactionRepository.FindAsync(t => t.TransactionId == callbackData.OrderCode);
        //        if (transaction != null)
        //        {
        //            transaction.Status = callbackData.Status;
        //            transaction.ListingId = transactionInfo.ListingId;
        //            transaction.TierId = transactionInfo.TierId;
        //            transaction.Amount = callbackData.Amount;
        //            transaction.TransactionDate = callbackData.TransactionDate;

        //            await _transactionRepository.UpdateAsync(transaction);
        //            await _unitOfWork.SaveChangesWithTransactionAsync();
        //        }
        //    }

        //    return Ok();
        //}



        //[HttpGet("getPayOSOrder/{orderCode}")]
        //public async Task<IActionResult> GetOrder([FromRoute] int orderCode)
        //{
        //    try
        //    {
        //        PaymentLinkInformation paymentLinkInformation = await _payOS.getPaymentLinkInformation(orderCode);
        //        return Ok(new MessageResponse(0, "Ok", paymentLinkInformation));
        //    }
        //    catch (Exception exception)
        //    {
        //        Console.WriteLine(exception);
        //        return Ok(new MessageResponse(-1, "fail", null));
        //    }
        //}

        //[HttpPut("cancelOrder/{orderCode}")]
        //public async Task<IActionResult> CancelOrder([FromRoute] int orderCode, string reason)
        //{
        //    try
        //    {
        //        PaymentLinkInformation paymentLinkInformation = await _payOS.cancelPaymentLink(orderCode, reason);
        //        return Ok(new MessageResponse(0, "Ok", paymentLinkInformation));
        //    }
        //    catch (Exception exception)
        //    {
        //        Console.WriteLine(exception);
        //        return Ok(new MessageResponse(-1, "fail", null));
        //    }
        //}

        //[HttpPost("confirm-webhook")]
        //public async Task<IActionResult> ConfirmWebhook(ConfirmWebhook body)
        //{
        //    try
        //    {
        //        await _payOS.confirmWebhook(body.webhook_url);
        //        return Ok(new MessageResponse(0, "Ok", null));
        //    }
        //    catch (Exception exception)
        //    {
        //        Console.WriteLine(exception);
        //        return Ok(new MessageResponse(-1, "fail", null));
        //    }
        //}

        //[HttpPost("CheckOrder")]
        //public async Task<IActionResult> CheckOrder([FromBody] CheckOrderRequest request)
        //{
        //    int orderCode = request.OrderCode;
        //    try
        //    {
        //        // Get the current user's email from the JWT token
        //        var userEmail = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
        //        if (string.IsNullOrEmpty(userEmail))
        //        {
        //            return Unauthorized("User not authenticated");
        //        }

        //        // Find the user in the database
        //        var user = await _accountRepository.FindAsync(u => u.Email == userEmail);
        //        if (user == null)
        //        {
        //            return NotFound("User not found");
        //        }

        //        PaymentLinkInformation paymentLinkInformation = await _payOS.getPaymentLinkInformation(orderCode);

        //        if (paymentLinkInformation.status == "PAID")
        //        {
        //            return Ok(new MessageResponse(0, "Transaction Complete", new
        //            {
        //                paymentInfo = paymentLinkInformation,
        //            }));

        //        }
        //        else
        //        {
        //            return Ok(new MessageResponse(0, "Payment not completed yet", new { paymentInfo = paymentLinkInformation }));
        //        }
        //    }
        //    catch (System.Exception exception)
        //    {
        //        Console.WriteLine(exception);
        //        return Ok(new MessageResponse(-1, "fail", null));
        //    }
        //}
        private readonly ITransactionService _transactionService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TransactionController(ITransactionService transactionService, IHttpContextAccessor httpContextAccessor)
        {
            _transactionService = transactionService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("CreatePayOSLink")]
        public async Task<IActionResult> CreatePaymentLink(CreatePaymentLinkRequest body, [FromQuery] int listingId, [FromQuery] int tierId)
        {
            var userEmail = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated");
            }

            var result = await _transactionService.CreatePaymentLink(body, listingId, tierId, userEmail);
            return Ok(result);
        }

        //[HttpPost("PaymentCallback")]
        //public async Task<IActionResult> PaymentCallback([FromBody] PaymentCallbackData callbackData)
        //{
        //    var result = await _transactionService.PaymentCallback(callbackData);
        //    return Ok(result);
        //}

        [HttpGet("getPayOSOrder/{orderCode}")]
        public async Task<IActionResult> GetOrder([FromRoute] int orderCode)
        {
            var result = await _transactionService.GetOrder(orderCode);
            return Ok(result);
        }

        [HttpPut("cancelOrder/{orderCode}")]
        public async Task<IActionResult> CancelOrder([FromRoute] int orderCode, string reason)
        {
            var result = await _transactionService.CancelOrder(orderCode, reason);
            return Ok(result);
        }

        [HttpPost("confirm-webhook")]
        public async Task<IActionResult> ConfirmWebhook(ConfirmWebhook body)
        {
            var result = await _transactionService.ConfirmWebhook(body);
            return Ok(result);
        }

        [HttpPost("CheckOrder")]
        public async Task<IActionResult> CheckOrder([FromBody] CheckOrderRequest request)
        {
            var userEmail = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated");
            }

            var result = await _transactionService.CheckOrder(request, userEmail);
            return Ok(result);
        }




    }
}
