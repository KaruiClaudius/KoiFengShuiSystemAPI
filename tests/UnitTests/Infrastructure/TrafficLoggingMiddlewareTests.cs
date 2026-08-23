using System.Security.Claims;
using KoiFengShuiSystem.Host.Middleware;
using KoiFengShuiSystem.Shared.Infrastructure.Background;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Infrastructure;

public class TrafficLoggingMiddlewareTests
{
    [Fact]
    public async Task Invoke_CallsNextBeforeEnqueuing()
    {
        var sequence = new List<string>();
        var sink = new RecordingSink(sequence);
        var context = CreateContext();
        var middleware = CreateMiddleware(
            _ =>
            {
                sequence.Add("next");
                return Task.CompletedTask;
            },
            sink);

        await middleware.InvokeAsync(context);

        Assert.Equal(new[] { "next", "enqueue" }, sequence);
    }

    [Fact]
    public async Task Invoke_RecordsStatusCodeAfterPipelineRan()
    {
        var sink = new RecordingSink();
        var context = CreateContext();
        var middleware = CreateMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            },
            sink);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal(StatusCodes.Status404NotFound, entry.StatusCode);
    }

    [Fact]
    public async Task Invoke_BuildsEntryFromRequestContextAndAuthenticatedUser()
    {
        var sink = new RecordingSink();
        var context = CreateContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.7");
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/koi/compatibility";
        context.Request.Headers.UserAgent = "test-agent";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "42") },
            authenticationType: "Test"));

        var middleware = CreateMiddleware(_ => Task.CompletedTask, sink);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(sink.Entries);
        Assert.True(entry.IsRegistered);
        Assert.Equal(42, entry.AccountId);
        Assert.Equal("203.0.113.7", entry.IpAddress);
        Assert.Equal(HttpMethods.Post, entry.RequestMethod);
        Assert.Equal("/api/koi/compatibility", entry.RequestPath);
        Assert.Equal("test-agent", entry.UserAgent);
    }

    [Fact]
    public async Task Invoke_AnonymousRequest_LogsWithoutAccount()
    {
        var sink = new RecordingSink();
        var context = CreateContext();

        var middleware = CreateMiddleware(_ => Task.CompletedTask, sink);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(sink.Entries);
        Assert.False(entry.IsRegistered);
        Assert.Null(entry.AccountId);
    }

    [Fact]
    public async Task Invoke_NextThrows_PropagatesWithoutEnqueuing()
    {
        var sink = new RecordingSink();
        var context = CreateContext();
        var middleware = CreateMiddleware(
            _ => throw new InvalidOperationException("pipeline exploded"),
            sink);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));

        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task Invoke_SinkEnqueueThrows_RequestStillCompletes()
    {
        var sink = new ThrowingSink();
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask, sink);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static TrafficLoggingMiddleware CreateMiddleware(
        RequestDelegate next,
        ITrafficSink sink) =>
        new(next, Mock.Of<ILogger<TrafficLoggingMiddleware>>(), sink);

    private static DefaultHttpContext CreateContext() => new();

    private sealed class RecordingSink : ITrafficSink
    {
        private readonly List<string>? _sequence;

        public RecordingSink() => _sequence = null;

        public RecordingSink(List<string> sequence) => _sequence = sequence;

        public List<TrafficLogEntry> Entries { get; } = new();

        public void Enqueue(TrafficLogEntry entry)
        {
            _sequence?.Add("enqueue");
            Entries.Add(entry);
        }
    }

    private sealed class ThrowingSink : ITrafficSink
    {
        public void Enqueue(TrafficLogEntry entry) =>
            throw new InvalidOperationException("sink unavailable");
    }
}
