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
        private readonly ITransactionService _transactionService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TransactionController(ITransactionService transactionService, IHttpContextAccessor httpContextAccessor)
        {
            _transactionService = transactionService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("CreatePayOSLink")]
        public async Task<IActionResult> CreatePaymentLink(CreatePaymentLinkRequest body)
        {
            var userEmail = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated");
            }

            var result = await _transactionService.CreatePaymentLink(body, userEmail);
            return Ok(result);
        }

        

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
