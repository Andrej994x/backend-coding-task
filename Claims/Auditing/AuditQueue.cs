using System.Threading.Channels;

namespace Claims.Auditing;

/// <summary>
/// In-memory queue for audit events. The HTTP request only adds an event here;
/// <see cref="AuditBackgroundService"/> saves it to SQL Server later.
/// </summary>
public class AuditQueue : IAuditer
{
    public const int DefaultCapacity = 1000;

    private readonly Channel<AuditEntry> _channel;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuditQueue> _logger;

    public AuditQueue(TimeProvider timeProvider, ILogger<AuditQueue> logger, int capacity = DefaultCapacity)
    {
        _timeProvider = timeProvider;
        _logger = logger;
        // Bounded queue: when it is full, writers wait for free space (backpressure) instead of losing events.
        _channel = Channel.CreateBounded<AuditEntry>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    public ChannelReader<AuditEntry> Reader => _channel.Reader;

    public ValueTask AuditClaimAsync(string claimId, string httpRequestType) =>
        EnqueueAsync(AuditEntityType.Claim, claimId, httpRequestType);

    public ValueTask AuditCoverAsync(string coverId, string httpRequestType) =>
        EnqueueAsync(AuditEntityType.Cover, coverId, httpRequestType);

    /// <summary>Stops accepting new events. Events already in the queue can still be read.</summary>
    public void Complete() => _channel.Writer.TryComplete();

    private async ValueTask EnqueueAsync(AuditEntityType entityType, string entityId, string httpRequestType)
    {
        // Take the time before a possible wait, so it shows when the operation happened.
        var entry = new AuditEntry(entityType, entityId, httpRequestType, _timeProvider.GetUtcNow().UtcDateTime);
        try
        {
            await _channel.Writer.WriteAsync(entry);
        }
        catch (ChannelClosedException)
        {
            // Only happens during shutdown. The business operation already succeeded, so do not fail the request.
            _logger.LogWarning("Audit queue is closed. Audit entry was not saved: {@AuditEntry}", entry);
        }
    }
}
