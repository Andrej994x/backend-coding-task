using Claims.Auditing;
using Claims.Domain;
using Claims.Persistence;

namespace Claims.Services;

/// <summary>Business operations for covers.</summary>
public class CoverService(ICoverRepository coverRepository, IAuditer auditer, PremiumCalculator premiumCalculator)
{
    public Task<IReadOnlyList<Cover>> GetAllAsync() => coverRepository.GetAllAsync();

    public Task<Cover?> GetByIdAsync(string id) => coverRepository.GetByIdAsync(id);

    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType) =>
        premiumCalculator.Calculate(startDate, endDate, coverType);

    public async Task<Cover> CreateAsync(Cover cover)
    {
        // Id and premium are always set by the server, values sent by the client are ignored.
        cover.Id = Guid.NewGuid().ToString();
        cover.Premium = premiumCalculator.Calculate(cover.StartDate, cover.EndDate, cover.Type);
        await coverRepository.AddAsync(cover);
        auditer.AuditCover(cover.Id, "POST");
        return cover;
    }

    public async Task DeleteAsync(string id)
    {
        auditer.AuditCover(id, "DELETE");
        await coverRepository.DeleteAsync(id);
    }
}
