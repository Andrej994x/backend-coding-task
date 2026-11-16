namespace Claims.Auditing;

/// <summary>
/// Reads audit events from <see cref="AuditQueue"/> and saves them to SQL Server,
/// outside of the HTTP request.
/// </summary>
public class AuditBackgroundService(
    AuditQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditBackgroundService> logger) : BackgroundService
{
    // Cancelled only when the host shutdown timeout expires. Until then, the queue is drained.
    private readonly CancellationTokenSource _abort = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // The loop ends when the queue is completed and empty, so events queued before shutdown are still saved.
            await foreach (var entry in queue.Reader.ReadAllAsync(_abort.Token))
            {
                try
                {
                    await SaveAsync(entry, _abort.Token);
                }
                catch (Exception ex) when (!_abort.IsCancellationRequested)
                {
                    // One failed event must not stop the processing of the next events.
                    logger.LogError(ex, "Failed to save audit entry {@AuditEntry}", entry);
                }
            }
        }
        catch (OperationCanceledException) when (_abort.IsCancellationRequested)
        {
            logger.LogWarning("Shutdown timeout reached. At least {Count} audit entries were not saved.", queue.Reader.Count);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // The web server is already stopped here, so no new events arrive. Close the queue and let it drain.
        queue.Complete();
        await using var registration = cancellationToken.Register(_abort.Cancel);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _abort.Dispose();
        base.Dispose();
    }

    private async Task SaveAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        // AuditContext is scoped and not thread safe, so use a new scope for each audit entry.
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();

        switch (entry.EntityType)
        {
            case AuditEntityType.Claim:
                context.ClaimAudits.Add(new ClaimAudit
                {
                    ClaimId = entry.EntityId,
                    HttpRequestType = entry.HttpRequestType,
                    Created = entry.Created
                });
                break;
            case AuditEntityType.Cover:
                context.CoverAudits.Add(new CoverAudit
                {
                    CoverId = entry.EntityId,
                    HttpRequestType = entry.HttpRequestType,
                    Created = entry.Created
                });
                break;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
