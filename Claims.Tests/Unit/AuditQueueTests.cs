using Claims.Auditing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Claims.Tests.Unit;

public class AuditQueueTests
{
    private static readonly DateTimeOffset Now = new(2027, 5, 1, 10, 30, 0, TimeSpan.Zero);

    private static AuditQueue CreateQueue(int capacity = AuditQueue.DefaultCapacity) =>
        new(new FixedTimeProvider(Now), NullLogger<AuditQueue>.Instance, capacity);

    [Fact]
    public async Task Claim_and_cover_events_are_queued_with_utc_time()
    {
        var queue = CreateQueue();

        await queue.AuditClaimAsync("claim-1", "POST");
        await queue.AuditCoverAsync("cover-1", "DELETE");

        Assert.True(queue.Reader.TryRead(out var first));
        Assert.True(queue.Reader.TryRead(out var second));
        Assert.Equal(new AuditEntry(AuditEntityType.Claim, "claim-1", "POST", Now.UtcDateTime), first);
        Assert.Equal(new AuditEntry(AuditEntityType.Cover, "cover-1", "DELETE", Now.UtcDateTime), second);
        Assert.Equal(DateTimeKind.Utc, first.Created.Kind);
    }

    [Fact]
    public async Task Writer_waits_when_queue_is_full_and_continues_after_a_read()
    {
        var queue = CreateQueue(capacity: 1);
        await queue.AuditClaimAsync("claim-1", "POST");

        var waitingWrite = queue.AuditClaimAsync("claim-2", "POST").AsTask();
        Assert.False(waitingWrite.IsCompleted);

        Assert.True(queue.Reader.TryRead(out _));
        await waitingWrite.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(queue.Reader.TryRead(out var entry));
        Assert.Equal("claim-2", entry.EntityId);
    }

    [Fact]
    public async Task Writing_after_complete_does_not_throw()
    {
        var queue = CreateQueue();
        queue.Complete();

        await queue.AuditClaimAsync("claim-1", "POST");

        Assert.False(queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task Writer_waiting_on_full_queue_does_not_throw_when_queue_is_completed()
    {
        var queue = CreateQueue(capacity: 1);
        await queue.AuditClaimAsync("claim-1", "POST");
        var waitingWrite = queue.AuditClaimAsync("claim-2", "POST").AsTask();

        queue.Complete();

        await waitingWrite.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }
}
