using Claims.Auditing;
using Claims.Domain;
using Claims.Services;
using Claims.Validation;
using Xunit;

namespace Claims.Tests.Unit;

/// <summary>
/// Tests what the service does with valid and invalid claims: saving and auditing.
/// All boundary values of the rules are tested in <see cref="ClaimValidatorTests"/>.
/// </summary>
public class ClaimServiceTests
{
    private static readonly DateTime CoverStart = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CoverEnd = new(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryClaimRepository _claims = new();
    private readonly InMemoryCoverRepository _covers = new();
    private readonly RecordingAuditer _auditer = new();
    private readonly ClaimService _service;

    public ClaimServiceTests()
    {
        _service = new ClaimService(_claims, _covers, _auditer);
        _covers.Items.Add("cover-1", new Cover { Id = "cover-1", StartDate = CoverStart, EndDate = CoverEnd, Type = CoverType.Yacht });
    }

    private static Claim NewClaim(decimal damageCost = 5_000m, DateTime? created = null) => new()
    {
        CoverId = "cover-1",
        Created = created ?? CoverStart.AddDays(10),
        Name = "Hull damage",
        Type = ClaimType.Collision,
        DamageCost = damageCost
    };

    [Fact]
    public async Task Create_valid_claim_saves_it_with_a_new_id_and_audits_post()
    {
        var created = await _service.CreateAsync(NewClaim());

        Assert.True(Guid.TryParse(created.Id, out _));
        Assert.Same(created, _claims.Items[created.Id]);
        Assert.Equal([new AuditCall(AuditEntityType.Claim, created.Id, "POST")], _auditer.Calls);
    }

    [Theory]
    [InlineData("2027-01-01")] // first day of the cover
    [InlineData("2027-06-30")] // last day of the cover
    public async Task Create_claim_on_cover_boundary_is_saved(string created)
    {
        var claim = await _service.CreateAsync(NewClaim(created: DateTime.SpecifyKind(DateTime.Parse(created), DateTimeKind.Utc)));

        Assert.Contains(claim.Id, _claims.Items.Keys);
    }

    [Fact]
    public async Task Create_claim_with_damage_cost_of_exactly_100000_is_saved()
    {
        var claim = await _service.CreateAsync(NewClaim(damageCost: 100_000m));

        Assert.Contains(claim.Id, _claims.Items.Keys);
    }

    public static TheoryData<string, Claim> InvalidClaims => new()
    {
        { nameof(Claim.DamageCost), NewClaim(damageCost: 100_000.01m) },
        { nameof(Claim.Created), NewClaim(created: CoverStart.AddDays(-1)) },
        { nameof(Claim.Created), NewClaim(created: CoverEnd.AddDays(1)) },
        { nameof(Claim.CoverId), new Claim { CoverId = "missing", Created = CoverStart, Name = "n", DamageCost = 1 } }
    };

    [Theory]
    [MemberData(nameof(InvalidClaims))]
    public async Task Invalid_claim_is_rejected_and_neither_saved_nor_audited(string expectedErrorField, Claim claim)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(claim));

        Assert.Contains(expectedErrorField, exception.Errors.Keys);
        Assert.Empty(_claims.Items);
        Assert.Empty(_auditer.Calls);
    }

    [Fact]
    public async Task Claim_is_not_audited_when_saving_fails()
    {
        _claims.FailOnAdd = new InvalidOperationException("Database is not available");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(NewClaim()));

        Assert.Empty(_auditer.Calls);
    }

    [Fact]
    public async Task Get_existing_claim_returns_it_without_auditing()
    {
        var created = await _service.CreateAsync(NewClaim());
        _auditer.Calls.Clear();

        var fetched = await _service.GetByIdAsync(created.Id);

        Assert.Same(created, fetched);
        Assert.Empty(_auditer.Calls);
    }

    [Fact]
    public async Task Get_nonexistent_claim_returns_null()
    {
        Assert.Null(await _service.GetByIdAsync("missing"));
    }

    [Fact]
    public async Task Delete_existing_claim_removes_it_and_audits_delete()
    {
        var created = await _service.CreateAsync(NewClaim());
        _auditer.Calls.Clear();

        var deleted = await _service.DeleteAsync(created.Id);

        Assert.True(deleted);
        Assert.Empty(_claims.Items);
        Assert.Equal([new AuditCall(AuditEntityType.Claim, created.Id, "DELETE")], _auditer.Calls);
    }

    [Fact]
    public async Task Delete_nonexistent_claim_returns_false_and_is_not_audited()
    {
        var deleted = await _service.DeleteAsync("missing");

        Assert.False(deleted);
        Assert.Empty(_auditer.Calls);
    }
}
