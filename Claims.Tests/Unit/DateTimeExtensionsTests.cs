using Claims.Contracts;
using Xunit;

namespace Claims.Tests.Unit;

public class DateTimeExtensionsTests
{
    [Fact]
    public void Date_without_offset_is_treated_as_utc()
    {
        var result = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AsUtc();

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void Date_with_offset_is_converted_to_utc_and_can_move_to_the_previous_day()
    {
        var local = DateTimeOffset.Parse("2027-01-01T01:00:00+02:00").LocalDateTime;

        Assert.Equal(new DateTime(2026, 12, 31, 23, 0, 0, DateTimeKind.Utc), local.AsUtc());
    }
}
