namespace KoiFengShuiSystem.Shared.Infrastructure.Background;

public sealed class TrafficLogEntry
{
    public DateTime Timestamp { get; init; }

    public bool IsRegistered { get; init; }

    public int? AccountId { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? RequestPath { get; init; }

    public string? RequestMethod { get; init; }

    public int StatusCode { get; init; }
}
