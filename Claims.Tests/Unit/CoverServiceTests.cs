using Claims.Auditing;
using Claims.Domain;
using Claims.Services;
using Claims.Validation;
using Xunit;

namespace Claims.Tests.Unit;

/// <summary>
/// Tests what the service does with valid and invalid covers: saving, pricing and auditing.
/// All boundary values of the rules are tested in <see cref="CoverValidatorTests"/>.
/// </summary>
public class CoverServiceTests
{
    private static readonly DateTime Today = new(2027, 3, 10, 0, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryCoverRepository _covers = new();
    private readonly RecordingAuditer _auditer = new();
    private readonly PremiumCalculator _calculator = new();
    private readonly CoverService _service;

    public CoverServiceTests()
    {
        var validator = new CoverValidator(new FixedTimeProvider(Today.AddHours(9)));
        _service = new CoverService(_covers, _auditer, validator, _calculator);
    }

    private static Cover NewCover(DateTime start, DateTime end) =>
        new() { StartDate = start, EndDate = end, Type = CoverType.Tanker };

    [Fact]
    public async Task Create_valid_cover_saves_it_with_id_and_premium_and_audits_post()
    {
        var created = await _service.CreateAsync(NewCover(Today.AddDays(1), Today.AddDays(91)));

        Assert.True(Guid.TryParse(created.Id, out _));
        Assert.Equal(_calculator.Calculate(created.StartDate, created.EndDate, created.Type), created.Premium);
        Assert.Same(created, _covers.Items[created.Id]);
        Assert.Equal([new AuditCall(AuditEntityType.Cover, created.Id, "POST")], _auditer.Calls);
    }

    [Fact]
    public async Task Create_cover_starting_today_is_saved()
    {
        var created = await _service.CreateAsync(NewCover(Today, Today.AddDays(30)));

        Assert.Contains(created.Id, _covers.Items.Keys);
    }

    [Fact]
    public async Task Create_cover_of_exactly_one_year_is_saved()
    {
        var created = await _service.CreateAsync(NewCover(Today, Today.AddYears(1)));

        Assert.Contains(created.Id, _covers.Items.Keys);
    }

    public static TheoryData<string, DateTime, DateTime> InvalidPeriods => new()
    {
        { nameof(Cover.StartDate), Today.AddDays(-1), Today.AddDays(30) },        // starts in the past
        { nameof(Cover.EndDate), Today, Today.AddYears(1).AddDays(1) },            // longer than one year
        { nameof(Cover.EndDate), Today.AddDays(10), Today.AddDays(5) }             // ends before it starts
    };

    [Theory]
    [MemberData(nameof(InvalidPeriods))]
    public async Task Invalid_cover_is_rejected_and_neither_saved_nor_audited(string expectedErrorField, DateTime start, DateTime end)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(NewCover(start, end)));

        Assert.Contains(expectedErrorField, exception.Errors.Keys);
        Assert.Empty(_covers.Items);
        Assert.Empty(_auditer.Calls);
    }

    [Fact]
    public void ComputePremium_returns_the_price_without_saving_or_auditing()
    {
        var premium = _service.ComputePremium(Today, Today.AddDays(30), CoverType.Yacht);

        Assert.Equal(_calculator.Calculate(Today, Today.AddDays(30), CoverType.Yacht), premium);
        Assert.Empty(_covers.Items);
        Assert.Empty(_auditer.Calls);
    }

    [Fact]
    public void ComputePremium_with_invalid_period_is_rejected()
    {
        Assert.Throws<ValidationException>(() => _service.ComputePremium(Today, Today.AddDays(-1), CoverType.Yacht));
    }

    [Fact]
    public async Task Get_existing_cover_returns_it_without_auditing()
    {
        var created = await _service.CreateAsync(NewCover(Today, Today.AddDays(30)));
        _auditer.Calls.Clear();

        Assert.Same(created, await _service.GetByIdAsync(created.Id));
        Assert.Empty(_auditer.Calls);
    }

    [Fact]
    public async Task Get_nonexistent_cover_returns_null()
    {
        Assert.Null(await _service.GetByIdAsync("missing"));
    }

    [Fact]
    public async Task Delete_existing_cover_removes_it_and_audits_delete()
    {
        var created = await _service.CreateAsync(NewCover(Today, Today.AddDays(30)));
        _auditer.Calls.Clear();

        var deleted = await _service.DeleteAsync(created.Id);

        Assert.True(deleted);
        Assert.Empty(_covers.Items);
        Assert.Equal([new AuditCall(AuditEntityType.Cover, created.Id, "DELETE")], _auditer.Calls);
    }

    [Fact]
    public async Task Delete_nonexistent_cover_returns_false_and_is_not_audited()
    {
        Assert.False(await _service.DeleteAsync("missing"));
        Assert.Empty(_auditer.Calls);
    }
}
