using Claims.Auditing;
using Claims.Domain;
using Claims.Persistence;

namespace Claims.Tests;

// Small hand-written test doubles. The interfaces are tiny, so a mocking library is not needed.

public class InMemoryClaimRepository : IClaimRepository
{
    public Dictionary<string, Claim> Items { get; } = new();

    /// <summary>When set, AddAsync throws this exception, like a failing database.</summary>
    public Exception? FailOnAdd { get; set; }

    public Task<IReadOnlyList<Claim>> GetAllAsync() => Task.FromResult<IReadOnlyList<Claim>>(Items.Values.ToList());

    public Task<Claim?> GetByIdAsync(string id) => Task.FromResult(Items.GetValueOrDefault(id));

    public Task AddAsync(Claim claim)
    {
        if (FailOnAdd is not null)
        {
            throw FailOnAdd;
        }

        Items.Add(claim.Id, claim);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id) => Task.FromResult(Items.Remove(id));
}

public class InMemoryCoverRepository : ICoverRepository
{
    public Dictionary<string, Cover> Items { get; } = new();

    public Task<IReadOnlyList<Cover>> GetAllAsync() => Task.FromResult<IReadOnlyList<Cover>>(Items.Values.ToList());

    public Task<Cover?> GetByIdAsync(string id) => Task.FromResult(Items.GetValueOrDefault(id));

    public Task AddAsync(Cover cover)
    {
        Items.Add(cover.Id, cover);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id) => Task.FromResult(Items.Remove(id));
}

public record AuditCall(AuditEntityType EntityType, string EntityId, string HttpRequestType);

public class RecordingAuditer : IAuditer
{
    public List<AuditCall> Calls { get; } = [];

    public ValueTask AuditClaimAsync(string claimId, string httpRequestType)
    {
        Calls.Add(new AuditCall(AuditEntityType.Claim, claimId, httpRequestType));
        return ValueTask.CompletedTask;
    }

    public ValueTask AuditCoverAsync(string coverId, string httpRequestType)
    {
        Calls.Add(new AuditCall(AuditEntityType.Cover, coverId, httpRequestType));
        return ValueTask.CompletedTask;
    }
}
