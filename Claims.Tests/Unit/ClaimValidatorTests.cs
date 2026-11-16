using Claims.Domain;
using Claims.Validation;
using Xunit;

namespace Claims.Tests.Unit;

public class ClaimValidatorTests
{
    private static readonly Cover Cover = new()
    {
        Id = "cover-1",
        StartDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        Type = CoverType.Yacht
    };

    private static Claim ValidClaim() => new()
    {
        CoverId = Cover.Id,
        Created = new DateTime(2027, 3, 15, 0, 0, 0, DateTimeKind.Utc),
        Name = "Hull damage",
        Type = ClaimType.Collision,
        DamageCost = 5_000m
    };

    [Fact]
    public void Valid_claim_has_no_errors()
    {
        Assert.Empty(ClaimValidator.Validate(ValidClaim(), Cover));
    }

    [Fact]
    public void DamageCost_of_exactly_100000_is_allowed()
    {
        var claim = ValidClaim();
        claim.DamageCost = 100_000m;

        Assert.Empty(ClaimValidator.Validate(claim, Cover));
    }

    [Fact]
    public void DamageCost_above_100000_is_rejected()
    {
        var claim = ValidClaim();
        claim.DamageCost = 100_000.01m;

        var errors = ClaimValidator.Validate(claim, Cover);

        Assert.Equal([nameof(Claim.DamageCost)], errors.Keys);
    }

    [Fact]
    public void Missing_cover_is_rejected()
    {
        var errors = ClaimValidator.Validate(ValidClaim(), cover: null);

        Assert.Equal([nameof(Claim.CoverId)], errors.Keys);
    }

    [Theory]
    [InlineData("2027-01-01T00:00:00")] // first day of the cover
    [InlineData("2027-06-30T00:00:00")] // last day of the cover
    [InlineData("2027-06-30T23:59:59")] // time of day is ignored
    public void Created_on_cover_boundary_is_allowed(string created)
    {
        var claim = ValidClaim();
        claim.Created = DateTime.SpecifyKind(DateTime.Parse(created), DateTimeKind.Utc);

        Assert.Empty(ClaimValidator.Validate(claim, Cover));
    }

    [Theory]
    [InlineData("2026-12-31T23:59:59")] // before the cover starts
    [InlineData("2027-07-01T00:00:00")] // after the cover ends
    public void Created_outside_cover_period_is_rejected(string created)
    {
        var claim = ValidClaim();
        claim.Created = DateTime.SpecifyKind(DateTime.Parse(created), DateTimeKind.Utc);

        var errors = ClaimValidator.Validate(claim, Cover);

        Assert.Equal([nameof(Claim.Created)], errors.Keys);
    }

    [Fact]
    public void Unknown_claim_type_is_rejected()
    {
        var claim = ValidClaim();
        claim.Type = (ClaimType)99;

        Assert.Equal([nameof(Claim.Type)], ClaimValidator.Validate(claim, Cover).Keys);
    }
}
