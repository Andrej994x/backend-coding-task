using Claims.Auditing;
using Claims.Domain;
using Claims.Persistence;
using Claims.Validation;

namespace Claims.Services;

/// <summary>Business operations for covers.</summary>
public class CoverService(
    ICoverRepository coverRepository,
    IAuditer auditer,
    CoverValidator coverValidator,
    PremiumCalculator premiumCalculator)
{
    public Task<IReadOnlyList<Cover>> GetAllAsync() => coverRepository.GetAllAsync();

    public Task<Cover?> GetByIdAsync(string id) => coverRepository.GetByIdAsync(id);

    /// <exception cref="ValidationException">The period or cover type is not valid.</exception>
    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        // A quote uses the same rules as a new cover, because only such a cover can be bought.
        ValidationException.ThrowIfAny(coverValidator.Validate(startDate, endDate, coverType));
        return premiumCalculator.Calculate(startDate, endDate, coverType);
    }

    /// <exception cref="ValidationException">The cover breaks a business rule. Nothing is saved.</exception>
    public async Task<Cover> CreateAsync(Cover cover)
    {
        ValidationException.ThrowIfAny(coverValidator.Validate(cover.StartDate, cover.EndDate, cover.Type));

        cover.Id = Guid.NewGuid().ToString();
        cover.Premium = premiumCalculator.Calculate(cover.StartDate, cover.EndDate, cover.Type);
        await coverRepository.AddAsync(cover);
        auditer.AuditCover(cover.Id, "POST");
        return cover;
    }

    /// <summary>Returns false when the cover does not exist.</summary>
    public async Task<bool> DeleteAsync(string id)
    {
        auditer.AuditCover(id, "DELETE");
        return await coverRepository.DeleteAsync(id);
    }
}
