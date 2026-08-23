namespace KoiFengShuiSystem.Shared.Infrastructure.Background;

/// <summary>
/// Non-blocking intake for request-path traffic logging: implementations must
/// never perform I/O inside <see cref="Enqueue"/>.
/// </summary>
public interface ITrafficSink
{
    void Enqueue(TrafficLogEntry entry);
}
