using Claims.Auditing;
using Claims.Domain;
using Claims.Persistence;

namespace Claims.Services;

/// <summary>Business operations for claims.</summary>
public class ClaimService(IClaimRepository claimRepository, IAuditer auditer)
{
    public Task<IReadOnlyList<Claim>> GetAllAsync() => claimRepository.GetAllAsync();

    public Task<Claim?> GetByIdAsync(string id) => claimRepository.GetByIdAsync(id);

    public async Task<Claim> CreateAsync(Claim claim)
    {
        // The server always generates the id, so a value sent by the client is ignored.
        claim.Id = Guid.NewGuid().ToString();
        await claimRepository.AddAsync(claim);
        auditer.AuditClaim(claim.Id, "POST");
        return claim;
    }

    public async Task DeleteAsync(string id)
    {
        auditer.AuditClaim(id, "DELETE");
        await claimRepository.DeleteAsync(id);
    }
}
