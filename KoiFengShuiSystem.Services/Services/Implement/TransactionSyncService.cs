using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.payOS.Errors;
using Net.payOS.Types;
using Net.payOS;

using KoiFengShuiSystem.DataAccess.Models;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class TransactionSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TransactionSyncService> _logger;
        private readonly PayOS _payOS;
        private const int BatchSize = 100;

        public TransactionSyncService(IServiceProvider serviceProvider, ILogger<TransactionSyncService> logger, PayOS payOS)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _payOS = payOS;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncTransactions();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while syncing transactions");
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }


        private async Task SyncTransactions()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KoiFengShuiContext>();

            int latestId = await GetLatestId();
            int currentId = latestId;
            bool hasMoreTransactions = true;

            while (hasMoreTransactions)
            {
                int processedCount = 0;
                for (int i = 0; i < BatchSize; i++)
                {
                    try
                    {
                        int? transactionId = await GetTransactionIdById(dbContext, currentId);
                        if (!transactionId.HasValue)
                        {
                            hasMoreTransactions = false;
                            break;
                        }

                        var paymentInfo = await _payOS.getPaymentLinkInformation(transactionId.Value);

                        await SaveTransaction(dbContext, paymentInfo);
                        currentId--;
                        processedCount++;
                    }
                    catch (PayOSError ex) when (ex.Code == "429")
                    {
                        _logger.LogWarning("Rate limit reached. Pausing sync process.");
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        break;
                    }
                    catch (PayOSError ex) when (ex.Code == "14")
                    {
                        hasMoreTransactions = false;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing order with Id {currentId}");
                        currentId++;
                        continue;
                    }
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Processed {processedCount} transactions. Last processed ID: {currentId - 1}");

                if (processedCount == 0)
                {
                    hasMoreTransactions = false;
                }
            }

            _logger.LogInformation("Finished syncing all transactions");
        }




        private async Task<int> GetLatestId()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KoiFengShuiContext>();

            var latestTransaction = await dbContext.Transactions
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            return latestTransaction?.Id ?? 0;
        }

        private async Task<int?> GetTransactionIdById(KoiFengShuiContext dbContext, int id)
        {
            var transaction = await dbContext.Transactions
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();

            return transaction?.TransactionId;
        }

        private async Task SaveTransaction(KoiFengShuiContext dbContext, PaymentLinkInformation paymentInfo)
        {
            var existingTransaction = await dbContext.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == (int)paymentInfo.orderCode);

            if (existingTransaction == null)
            {
                var newTransaction = new DataAccess.Models.Transaction
                {
                    TransactionId = (int)paymentInfo.orderCode,
                    Amount = paymentInfo.amount,
                    Status = paymentInfo.status,
                    TransactionDate = DateTime.Parse(paymentInfo.createdAt),
                    //TierId = null, // Set this if applicable
                    //ListingId = null // Set this if applicable
                };

                dbContext.Transactions.Add(newTransaction);
            }
            else
            {
                existingTransaction.Status = paymentInfo.status;
                existingTransaction.TransactionDate = DateTime.Parse(paymentInfo.createdAt);
                dbContext.Entry(existingTransaction).State = EntityState.Modified;
            }
        }

    }
}
