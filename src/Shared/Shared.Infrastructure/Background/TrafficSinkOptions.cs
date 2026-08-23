namespace KoiFengShuiSystem.Shared.Infrastructure.Background;

public sealed class TrafficSinkOptions
{
    public const string SectionName = "TrafficSink";

    /// <summary>Maximum entries held in memory; beyond this the oldest are dropped.</summary>
    public int Capacity { get; set; } = 1000;

    /// <summary>Maximum rows persisted per SaveChanges.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>Window used to coalesce bursts into one batch.</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMilliseconds(500);
}
