using System.Threading.Channels;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KoiFengShuiSystem.Shared.Infrastructure.Background;

/// <summary>
/// Channel-backed traffic sink that moves traffic-log persistence off the HTTP
/// request path. Entries are buffered in a bounded channel (oldest dropped on
/// overflow) and flushed in batches, one SaveChanges per batch, through a scope
/// created per flush so the DbContext lifetime stays scoped.
/// </summary>
public sealed class BackgroundTrafficSink : BackgroundService, ITrafficSink
{
    private readonly Channel<TrafficLogEntry> _channel;
    private readonly TrafficSinkOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTrafficSink> _logger;

    public BackgroundTrafficSink(
        IOptions<TrafficSinkOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundTrafficSink> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        // DropOldest guarantees Enqueue always succeeds; the explicit pre-eviction
        // below exists to emit a warning when entries are sacrificed.
        var channelOptions = new BoundedChannelOptions(Math.Max(1, _options.Capacity))
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<TrafficLogEntry>(channelOptions);
    }

    public void Enqueue(TrafficLogEntry entry)
    {
        if (_channel.Reader.Count >= Math.Max(1, _options.Capacity) && _channel.Reader.TryRead(out var evicted))
        {
            _logger.LogWarning(
                "Traffic log buffer overflow: dropped oldest entry for {RequestPath}.",
                evicted.RequestPath);
        }

        if (!_channel.Writer.TryWrite(entry))
        {
            // Only possible once the writer is completed (shutdown); logging must never throw.
            _logger.LogWarning("Traffic log sink rejected an entry for {RequestPath}.", entry.RequestPath);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await FlushLoopAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown; StopAsync performs the final drain.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Traffic log flush loop terminated unexpectedly.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        try
        {
            var drained = await DrainPendingAsync(CancellationToken.None);
            if (drained > 0)
            {
                _logger.LogInformation("Flushed {Count} buffered traffic log entries during shutdown.", drained);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to drain the traffic log buffer during shutdown.");
        }
    }

    /// <summary>
    /// Test/shutdown hook: synchronously persists everything currently buffered.
    /// </summary>
    internal async Task<int> DrainPendingAsync(CancellationToken cancellationToken)
    {
        var total = 0;

        while (_channel.Reader.TryPeek(out _))
        {
            var batch = new List<TrafficLogEntry>(Math.Min(_options.BatchSize, Math.Max(1, _channel.Reader.Count)));
            while (batch.Count < Math.Max(1, _options.BatchSize) && _channel.Reader.TryRead(out var entry))
            {
                batch.Add(entry);
            }

            await WriteBatchAsync(batch, cancellationToken);
            total += batch.Count;
        }

        return total;
    }

    private async Task FlushLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = new List<TrafficLogEntry>(_options.BatchSize);

            ReadAvailable(batch);

            if (batch.Count == 0 && !await _channel.Reader.WaitToReadAsync(stoppingToken))
            {
                break; // channel completed and empty: nothing left to do
            }

            ReadAvailable(batch);

            if (_options.FlushInterval > TimeSpan.Zero && batch.Count < Math.Max(1, _options.BatchSize))
            {
                await TryCoalesceAsync(batch, stoppingToken);
            }

            await WriteBatchAsync(batch, CancellationToken.None);
        }
    }

    private void ReadAvailable(List<TrafficLogEntry> batch)
    {
        while (batch.Count < Math.Max(1, _options.BatchSize) && _channel.Reader.TryRead(out var entry))
        {
            batch.Add(entry);
        }
    }

    private async Task TryCoalesceAsync(List<TrafficLogEntry> batch, CancellationToken stoppingToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(_options.FlushInterval);

        try
        {
            if (await _channel.Reader.WaitToReadAsync(timeoutCts.Token))
            {
                ReadAvailable(batch);
            }
        }
        catch (OperationCanceledException)
        {
            // Coalesce window elapsed; flush whatever accumulated.
        }
    }

    private async Task WriteBatchAsync(List<TrafficLogEntry> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<KoiFengShuiContext>();

            foreach (var entry in batch)
            {
                context.TrafficLogs.Add(ToEntity(entry));
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist a batch of {Count} traffic log entries.", batch.Count);
        }
    }

    private static TrafficLog ToEntity(TrafficLogEntry entry) => new()
    {
        Timestamp = entry.Timestamp,
        IsRegistered = entry.IsRegistered,
        AccountId = entry.AccountId,
        IpAddress = entry.IpAddress,
        UserAgent = entry.UserAgent,
        RequestPath = entry.RequestPath,
        RequestMethod = entry.RequestMethod
    };
}
