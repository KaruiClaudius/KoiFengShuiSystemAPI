using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Net.payOS.Types;
using Net.payOS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class TransactionService : ITransactionService
    {
        private static readonly ConcurrentDictionary<int, (int ListingId, int TierId)> _pendingTransactions = new ConcurrentDictionary<int, (int, int)>();
        private readonly GenericRepository<Account> _accountRepository;
        private readonly GenericRepository<MarketplaceListing> _marketplaceListingRepository;
        private readonly GenericRepository<DataAccess.Models.Transaction> _transactionRepository;
        private readonly PayOS _payOS;
        private readonly IUnitOfWorkRepository _unitOfWork;

        public TransactionService(
            GenericRepository<Account> accountRepository,
            GenericRepository<MarketplaceListing> marketplaceListingRepository,
            GenericRepository<DataAccess.Models.Transaction> transactionRepository,
            PayOS payOS,
            IUnitOfWorkRepository unitOfWork)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _payOS = payOS;
            _marketplaceListingRepository = marketplaceListingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<MessageResponse> CreatePaymentLink(CreatePaymentLinkRequest body, int listingId, int tierId, string userEmail)
        {
            try
            {
                var user = await _accountRepository.FindAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return new MessageResponse(-1, "User not found", null);
                }

                int orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));
                _pendingTransactions[orderCode] = (listingId, tierId);

                ItemData item = new ItemData(body.productName, 1, body.price);
                List<ItemData> items = new List<ItemData> { item };

                string buyerName = !string.IsNullOrEmpty(body.buyerName) ? body.buyerName : user.FullName;
                string buyerEmail = !string.IsNullOrEmpty(body.buyerEmail) ? body.buyerEmail : userEmail;

                PaymentData paymentData = new PaymentData(
                    orderCode,
                    body.price,
                    body.description,
                    items,
                    body.cancelUrl,
                    body.returnUrl,
                    buyerName,
                    buyerEmail
                );

                CreatePaymentResult createPayment = await _payOS.createPaymentLink(paymentData);

                var paymentTransaction = new DataAccess.Models.Transaction
                {
                    TransactionId = orderCode,
                    AccountId = user.AccountId,
                    Amount = body.price,
                    Status = "PENDING",
                    TransactionDate = DateTime.UtcNow,
                    ListingId = listingId,
                    TierId = tierId
                };

                await _transactionRepository.CreateAsync(paymentTransaction);
                await _unitOfWork.SaveChangesWithTransactionAsync();

                var currentUserInfo = new
                {
                    user.AccountId,
                    user.FullName,
                    user.Email,
                    user.Phone,
                    user.Wallet
                };

                return new MessageResponse(0, "success", new
                {
                    paymentInfo = createPayment,
                    userInfo = currentUserInfo
                });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return new MessageResponse(-1, "fail", null);
            }
        }

        //public async Task<MessageResponse> PaymentCallback(PaymentCallbackData callbackData)
        //{
        //    if (_pendingTransactions.TryRemove(callbackData.OrderCode, out var transactionInfo))
        //    {
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

        //    return new MessageResponse(0, "OK", null);
        //}

        public async Task<MessageResponse> GetOrder(int orderCode)
        {
            try
            {
                PaymentLinkInformation paymentLinkInformation = await _payOS.getPaymentLinkInformation(orderCode);
                return new MessageResponse(0, "Ok", paymentLinkInformation);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return new MessageResponse(-1, "fail", null);
            }
        }

        public async Task<MessageResponse> CancelOrder(int orderCode, string reason)
        {
            try
            {
                PaymentLinkInformation paymentLinkInformation = await _payOS.cancelPaymentLink(orderCode, reason);
                return new MessageResponse(0, "Ok", paymentLinkInformation);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return new MessageResponse(-1, "fail", null);
            }
        }

        public async Task<MessageResponse> ConfirmWebhook(ConfirmWebhook body)
        {
            try
            {
                await _payOS.confirmWebhook(body.webhook_url);
                return new MessageResponse(0, "Ok", null);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return new MessageResponse(-1, "fail", null);
            }
        }
        public async Task<MessageResponse> CheckOrder(CheckOrderRequest request, string userEmail)
        {
            try
            {
                var user = await _accountRepository.FindAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return new MessageResponse(-1, "User not found", null);
                }

                PaymentLinkInformation paymentLinkInformation = await _payOS.getPaymentLinkInformation(request.OrderCode);

                if (paymentLinkInformation.status == "PAID")
                {
                    // Find the existing payment transaction
                    var paymentTransaction = await _transactionRepository
                        .FindByCondition(pt => pt.TransactionId == request.OrderCode)
                        .FirstOrDefaultAsync();

                    if (paymentTransaction == null)
                    {
                        return new MessageResponse(-1, "Transaction not found", null);
                    }

                    // Update transaction status and date
                    paymentTransaction.Status = "PAID";
                    paymentTransaction.TransactionDate = DateTime.UtcNow;
                    _transactionRepository.Update(paymentTransaction);
                    user.Wallet = (user.Wallet ?? 0) + paymentLinkInformation.amountPaid;
                    _accountRepository.Update(user);
                    await _unitOfWork.SaveChangesWithTransactionAsync();

                    var updatedUserInfo = new
                    {
                        user.AccountId,
                        user.FullName,
                        user.Email,
                        user.Phone,
                        user.Wallet
                    };
                    return new MessageResponse(0, "Transaction Complete", new { paymentInfo = paymentLinkInformation, userInfo = updatedUserInfo });
                }
                else
                {
                    return new MessageResponse(0, "Payment not completed yet", new { paymentInfo = paymentLinkInformation });
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return new MessageResponse(-1, "fail", null);
            }
        }

    }
}
