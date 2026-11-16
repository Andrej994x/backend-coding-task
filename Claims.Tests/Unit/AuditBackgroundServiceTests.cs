using Claims.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Claims.Tests.Unit;

/// <summary>Uses the EF in-memory provider instead of SQL Server, so no Docker is needed.</summary>
public class AuditBackgroundServiceTests
{
    private readonly AuditQueue _queue = new(TimeProvider.System, NullLogger<AuditQueue>.Instance);
    private readonly string _databaseName = Guid.NewGuid().ToString();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Queued_events_are_saved_and_the_queue_is_drained_on_stop()
    {
        await using var provider = BuildProvider();
        var service = CreateService(provider);
        await service.StartAsync(Ct);

        await _queue.AuditClaimAsync("claim-1", "POST");
        await _queue.AuditCoverAsync("cover-1", "DELETE");
        await service.StopAsync(Ct);

        await using var context = CreateContext();
        var claimAudit = Assert.Single(context.ClaimAudits);
        Assert.Equal(("claim-1", "POST"), (claimAudit.ClaimId, claimAudit.HttpRequestType));
        var coverAudit = Assert.Single(context.CoverAudits);
        Assert.Equal(("cover-1", "DELETE"), (coverAudit.CoverId, coverAudit.HttpRequestType));
    }

    [Fact]
    public async Task A_failing_event_is_skipped_and_the_next_event_is_still_saved()
    {
        // The first AuditContext cannot be created; later scopes get a working context.
        var attempts = 0;
        await using var provider = BuildProvider(services => services.AddScoped(_ =>
            Interlocked.Increment(ref attempts) == 1
                ? throw new InvalidOperationException("Database is not available")
                : CreateContext()));
        var service = CreateService(provider);
        await service.StartAsync(Ct);

        await _queue.AuditClaimAsync("claim-lost", "POST");
        await _queue.AuditClaimAsync("claim-saved", "POST");
        await service.StopAsync(Ct);

        await using var context = CreateContext();
        Assert.Equal("claim-saved", Assert.Single(context.ClaimAudits).ClaimId);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task Stop_gives_up_when_the_shutdown_timeout_expires()
    {
        await using var provider = BuildProvider(services => services.AddScoped<AuditContext>(_ => new HangingAuditContext()));
        var service = CreateService(provider);
        await service.StartAsync(Ct);
        await _queue.AuditClaimAsync("claim-1", "POST");

        using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await service.StopAsync(shutdownTimeout.Token);

        // The worker must really end, not keep running after the host has stopped.
        await service.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), Ct);
    }

    // A database that never answers.
    private sealed class HangingAuditContext()
        : AuditContext(new DbContextOptionsBuilder<AuditContext>().UseInMemoryDatabase("hanging").Options)
    {
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }
    }

    private AuditBackgroundService CreateService(ServiceProvider provider) =>
        new(_queue, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<AuditBackgroundService>.Instance);

    private ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        if (configure is null)
        {
            services.AddScoped(_ => CreateContext());
        }
        else
        {
            configure(services);
        }

        return services.BuildServiceProvider();
    }

    private AuditContext CreateContext() =>
        new(new DbContextOptionsBuilder<AuditContext>().UseInMemoryDatabase(_databaseName).Options);
}
