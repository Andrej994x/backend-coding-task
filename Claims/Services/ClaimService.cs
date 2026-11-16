using Claims.Auditing;
using Claims.Domain;
using Claims.Persistence;
using Claims.Validation;

namespace Claims.Services;

/// <summary>Business operations for claims.</summary>
public class ClaimService(IClaimRepository claimRepository, ICoverRepository coverRepository, IAuditer auditer)
{
    public Task<IReadOnlyList<Claim>> GetAllAsync() => claimRepository.GetAllAsync();

    public Task<Claim?> GetByIdAsync(string id) => claimRepository.GetByIdAsync(id);

    /// <exception cref="ValidationException">The claim breaks a business rule. Nothing is saved.</exception>
    public async Task<Claim> CreateAsync(Claim claim)
    {
        var cover = await coverRepository.GetByIdAsync(claim.CoverId);
        ValidationException.ThrowIfAny(ClaimValidator.Validate(claim, cover));

        claim.Id = Guid.NewGuid().ToString();
        await claimRepository.AddAsync(claim);
        await auditer.AuditClaimAsync(claim.Id, "POST");
        return claim;
    }

    /// <summary>Returns false when the claim does not exist.</summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var deleted = await claimRepository.DeleteAsync(id);
        // Only audit operations that really changed data.
        if (deleted)
        {
            await auditer.AuditClaimAsync(id, "DELETE");
        }

        return deleted;
    }
}
