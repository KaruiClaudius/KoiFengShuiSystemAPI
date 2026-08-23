using KoiFengShuiSystem.Shared.Infrastructure.Background;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace UnitTests.Infrastructure;

public class BackgroundTrafficSinkTests
{
    [Fact]
    public async Task DrainPendingAsync_PersistsEnqueuedEntries_InOneSaveChanges()
    {
        var scopeFactory = new FakeScopeFactory();
        using var sink = CreateSink(scopeFactory);
        var at = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);

        sink.Enqueue(new TrafficLogEntry
        {
            Timestamp = at,
            RequestPath = "/a",
            RequestMethod = "GET"
        });
        sink.Enqueue(new TrafficLogEntry
        {
            Timestamp = at.AddMilliseconds(1),
            RequestPath = "/b",
           RequestMethod = "POST",
            IsRegistered = true,
            AccountId = 7
        });

        await sink.DrainPendingAsync(CancellationToken.None);

        Assert.Equal(1, scopeFactory.Context.SaveChangesAsyncCalls);
        var rows = await scopeFactory.Context.TrafficLogs.OrderBy(row => row.RequestPath).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal("/a", rows[0].RequestPath);
        Assert.Equal("GET", rows[0].RequestMethod);
        Assert.False(rows[0].IsRegistered);
        Assert.Equal(at, rows[0].Timestamp);
        Assert.Equal("/b", rows[1].RequestPath);
        Assert.Equal("POST", rows[1].RequestMethod);
        Assert.True(rows[1].IsRegistered);
        Assert.Equal(7, rows[1].AccountId);
    }

    [Fact]
    public async Task DrainPendingAsync_ChunksLargeQueues_ByBatchSize()
    {
        var scopeFactory = new FakeScopeFactory();
        using var sink = CreateSink(scopeFactory, batchSize: 2);

        for (var i = 0; i < 5; i++)
        {
            sink.Enqueue(new TrafficLogEntry { Timestamp = DateTime.UtcNow, RequestPath = $"/p{i}" });
        }

        await sink.DrainPendingAsync(CancellationToken.None);

        // 5 entries with batch size 2 -> 3 flushes (2 + 2 + 1), one SaveChanges each.
        Assert.Equal(3, scopeFactory.Context.SaveChangesAsyncCalls);
        Assert.Equal(5, await scopeFactory.Context.TrafficLogs.CountAsync());
    }

    [Fact]
    public async Task DrainPendingAsync_EmptyChannel_WritesNothing()
    {
        var scopeFactory = new FakeScopeFactory();
        using var sink = CreateSink(scopeFactory);

        await sink.DrainPendingAsync(CancellationToken.None);

        Assert.Equal(0, scopeFactory.Context.SaveChangesAsyncCalls);
        Assert.Equal(0, await scopeFactory.Context.TrafficLogs.CountAsync());
    }

    [Fact]
    public async Task Enqueue_BeyondCapacity_DropsOldestWithoutThrowing()
    {
        var scopeFactory = new FakeScopeFactory();
        using var sink = CreateSink(scopeFactory, capacity: 2);

        sink.Enqueue(new TrafficLogEntry { Timestamp = DateTime.UtcNow, RequestPath = "/oldest" });
        sink.Enqueue(new TrafficLogEntry { Timestamp = DateTime.UtcNow, RequestPath = "/middle" });
        sink.Enqueue(new TrafficLogEntry { Timestamp = DateTime.UtcNow, RequestPath = "/newest" });

        await sink.DrainPendingAsync(CancellationToken.None);

        var paths = await scopeFactory.Context.TrafficLogs.Select(row => row.RequestPath).ToListAsync();
        Assert.DoesNotContain("/oldest", paths);
        Assert.Contains("/middle", paths);
        Assert.Contains("/newest", paths);
        Assert.Equal(2, paths.Count);
    }

    [Fact]
    public async Task StopAsync_DrainsRemainingEntries()
    {
        var scopeFactory = new FakeScopeFactory();
        var sink = CreateSink(scopeFactory);

        sink.Enqueue(new TrafficLogEntry { Timestamp = DateTime.UtcNow, RequestPath = "/pending-1" });
        sink.Enqueue(new TrafficLogEntry { Timestamp = DateTime.UtcNow, RequestPath = "/pending-2" });

        await sink.StopAsync(CancellationToken.None);

        Assert.Equal(2, await scopeFactory.Context.TrafficLogs.CountAsync());
    }

    private static BackgroundTrafficSink CreateSink(
        FakeScopeFactory scopeFactory,
        int capacity = 1000,
        int batchSize = 50) =>
        new(
            Options.Create(new TrafficSinkOptions
            {
                Capacity = capacity,
                BatchSize = batchSize,
                FlushInterval = TimeSpan.FromMilliseconds(500)
            }),
            scopeFactory,
            NullLogger<BackgroundTrafficSink>.Instance);

    private sealed class FakeScopeFactory : IServiceScopeFactory
    {
        public CountingContext Context { get; } = new(
            new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        public IServiceScope CreateScope() => new FakeScope(Context);

        private sealed class FakeScope(CountingContext context) : IServiceScope, IServiceProvider
        {
            public IServiceProvider ServiceProvider => this;

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(KoiFengShuiContext) ||
                    serviceType == typeof(CountingContext))
                {
                    return context;
                }

                return null;
            }

            public void Dispose()
            {
            }
        }
    }

    private sealed class CountingContext(DbContextOptions<KoiFengShuiContext> options)
        : KoiFengShuiContext(options)
    {
        public int SaveChangesAsyncCalls { get; private set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesAsyncCalls++;
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
