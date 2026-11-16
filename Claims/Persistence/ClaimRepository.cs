using Claims.Domain;
using Microsoft.EntityFrameworkCore;

namespace Claims.Persistence;

public interface IClaimRepository
{
    Task<IReadOnlyList<Claim>> GetAllAsync();
    Task<Claim?> GetByIdAsync(string id);
    Task AddAsync(Claim claim);

    /// <summary>Deletes the claim. Returns false when no claim with this id exists.</summary>
    Task<bool> DeleteAsync(string id);
}

public class ClaimRepository(ClaimsContext context) : IClaimRepository
{
    public async Task<IReadOnlyList<Claim>> GetAllAsync() =>
        await context.Claims.ToListAsync();

    public Task<Claim?> GetByIdAsync(string id) =>
        context.Claims.SingleOrDefaultAsync(claim => claim.Id == id);

    public async Task AddAsync(Claim claim)
    {
        context.Claims.Add(claim);
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var claim = await GetByIdAsync(id);
        if (claim is null)
        {
            return false;
        }

        context.Claims.Remove(claim);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request deleted the same claim between our read and our delete.
            return false;
        }
    }
}
