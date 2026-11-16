using Claims.Domain;
using Microsoft.EntityFrameworkCore;

namespace Claims.Persistence;

public interface ICoverRepository
{
    Task<IReadOnlyList<Cover>> GetAllAsync();
    Task<Cover?> GetByIdAsync(string id);
    Task AddAsync(Cover cover);

    /// <summary>Deletes the cover. Returns false when no cover with this id exists.</summary>
    Task<bool> DeleteAsync(string id);
}

public class CoverRepository(ClaimsContext context) : ICoverRepository
{
    public async Task<IReadOnlyList<Cover>> GetAllAsync() =>
        await context.Covers.ToListAsync();

    public Task<Cover?> GetByIdAsync(string id) =>
        context.Covers.SingleOrDefaultAsync(cover => cover.Id == id);

    public async Task AddAsync(Cover cover)
    {
        context.Covers.Add(cover);
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var cover = await GetByIdAsync(id);
        if (cover is null)
        {
            return false;
        }

        context.Covers.Remove(cover);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request deleted the same cover between our read and our delete.
            return false;
        }
    }
}
