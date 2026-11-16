using Claims.Domain;
using Claims.Validation;
using Xunit;

namespace Claims.Tests.Unit;

public class CoverValidatorTests
{
    // "Now" is in the middle of the day to show that the time of day does not matter.
    private static readonly DateTime Today = new(2027, 3, 10, 0, 0, 0, DateTimeKind.Utc);
    private readonly CoverValidator _validator = new(new FixedTimeProvider(Today.AddHours(15)));

    private Dictionary<string, string[]> Validate(DateTime start, DateTime end, CoverType type = CoverType.Tanker) =>
        _validator.Validate(start, end, type);

    [Fact]
    public void Valid_period_has_no_errors()
    {
        Assert.Empty(Validate(Today.AddDays(1), Today.AddDays(90)));
    }

    [Fact]
    public void StartDate_today_is_allowed()
    {
        Assert.Empty(Validate(Today, Today.AddDays(30)));
    }

    [Fact]
    public void StartDate_in_the_past_is_rejected()
    {
        var errors = Validate(Today.AddDays(-1), Today.AddDays(30));

        Assert.Equal([nameof(Cover.StartDate)], errors.Keys);
    }

    [Fact]
    public void Period_of_exactly_one_year_is_allowed()
    {
        Assert.Empty(Validate(Today, Today.AddYears(1)));
    }

    [Fact]
    public void Period_longer_than_one_year_is_rejected()
    {
        var errors = Validate(Today, Today.AddYears(1).AddDays(1));

        Assert.Equal([nameof(Cover.EndDate)], errors.Keys);
    }

    [Fact]
    public void One_year_from_leap_day_ends_on_28_February()
    {
        var validator = new CoverValidator(new FixedTimeProvider(new DateTime(2028, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        var start = new DateTime(2028, 2, 29, 0, 0, 0, DateTimeKind.Utc);

        Assert.Empty(validator.Validate(start, new DateTime(2029, 2, 28, 0, 0, 0, DateTimeKind.Utc), CoverType.Yacht));
        Assert.NotEmpty(validator.Validate(start, new DateTime(2029, 3, 1, 0, 0, 0, DateTimeKind.Utc), CoverType.Yacht));
    }

    [Theory]
    [InlineData(0)]  // same day
    [InlineData(-5)] // end before start
    public void EndDate_not_after_StartDate_is_rejected(int endOffsetDays)
    {
        var start = Today.AddDays(10);

        var errors = Validate(start, start.AddDays(endOffsetDays));

        Assert.Equal([nameof(Cover.EndDate)], errors.Keys);
    }

    [Fact]
    public void StartDate_in_year_9999_does_not_throw()
    {
        var start = new DateTime(9999, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Empty(Validate(start, start.AddDays(30)));
    }

    [Fact]
    public void Unknown_cover_type_is_rejected()
    {
        var errors = Validate(Today, Today.AddDays(30), (CoverType)99);

        Assert.Equal([nameof(Cover.Type)], errors.Keys);
    }
}
