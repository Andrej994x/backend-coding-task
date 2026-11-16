using Claims.Domain;
using Claims.Services;
using Xunit;

namespace Claims.Tests.Unit;

/// <summary>
/// Expected values follow the official rules: base day rate 1250 x type multiplier;
/// days 1-30 full price; days 31-180 discount 5% (Yacht) / 2% (others);
/// from day 181 a total discount of 8% (Yacht) / 3% (others).
/// </summary>
public class PremiumCalculatorTests
{
    private static readonly DateTime Start = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly PremiumCalculator _calculator = new();

    // Daily prices used below (full / days 31-180 / from day 181):
    // Yacht         1375 / 1306.25 / 1265
    // PassengerShip 1500 / 1470    / 1455
    // Tanker        1875 / 1837.5  / 1818.75
    // ContainerShip 1625 / 1592.5  / 1576.25 (BulkCarrier is the same)
    public static TheoryData<CoverType, int, decimal> Premiums => new()
    {
        { CoverType.Yacht, 1, 1_375m },
        { CoverType.Yacht, 30, 41_250m },
        { CoverType.Yacht, 31, 42_556.25m },
        { CoverType.Yacht, 180, 237_187.5m },
        { CoverType.Yacht, 181, 238_452.5m },
        { CoverType.Yacht, 365, 471_212.5m },
        { CoverType.Yacht, 366, 472_477.5m },

        { CoverType.PassengerShip, 1, 1_500m },
        { CoverType.PassengerShip, 30, 45_000m },
        { CoverType.PassengerShip, 31, 46_470m },
        { CoverType.PassengerShip, 180, 265_500m },
        { CoverType.PassengerShip, 181, 266_955m },
        { CoverType.PassengerShip, 365, 534_675m },
        { CoverType.PassengerShip, 366, 536_130m },

        { CoverType.Tanker, 1, 1_875m },
        { CoverType.Tanker, 30, 56_250m },
        { CoverType.Tanker, 31, 58_087.5m },
        { CoverType.Tanker, 180, 331_875m },
        { CoverType.Tanker, 181, 333_693.75m },
        { CoverType.Tanker, 365, 668_343.75m },
        { CoverType.Tanker, 366, 670_162.5m },

        { CoverType.ContainerShip, 1, 1_625m },
        { CoverType.ContainerShip, 30, 48_750m },
        { CoverType.ContainerShip, 31, 50_342.5m },
        { CoverType.ContainerShip, 180, 287_625m },
        { CoverType.ContainerShip, 181, 289_201.25m },
        { CoverType.ContainerShip, 365, 579_231.25m },
        { CoverType.ContainerShip, 366, 580_807.5m },

        { CoverType.BulkCarrier, 1, 1_625m },
        { CoverType.BulkCarrier, 30, 48_750m },
        { CoverType.BulkCarrier, 31, 50_342.5m },
        { CoverType.BulkCarrier, 180, 287_625m },
        { CoverType.BulkCarrier, 181, 289_201.25m },
        { CoverType.BulkCarrier, 365, 579_231.25m },
        { CoverType.BulkCarrier, 366, 580_807.5m }
    };

    [Theory]
    [MemberData(nameof(Premiums))]
    public void Premium_matches_the_official_rules(CoverType coverType, int days, decimal expected)
    {
        Assert.Equal(expected, _calculator.Calculate(Start, Start.AddDays(days), coverType));
    }

    [Fact]
    public void Premium_counts_calendar_days_and_ignores_the_time_of_day()
    {
        // From the evening of 1 January to the morning of 31 January is 30 calendar days.
        var start = new DateTime(2027, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2027, 1, 31, 6, 0, 0, DateTimeKind.Utc);

        Assert.Equal(41_250m, _calculator.Calculate(start, end, CoverType.Yacht));
    }

    [Fact]
    public void Short_period_over_midnight_is_one_day()
    {
        var start = new DateTime(2027, 1, 1, 23, 0, 0, DateTimeKind.Utc);

        Assert.Equal(1_375m, _calculator.Calculate(start, start.AddHours(2), CoverType.Yacht));
    }

    [Theory]
    [InlineData(0)]   // same moment
    [InlineData(-24)] // end before start
    [InlineData(5)]   // 5 hours, but on the same calendar day
    public void Period_shorter_than_one_calendar_day_is_rejected(int hours)
    {
        var start = new DateTime(2027, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(start, start.AddHours(hours), CoverType.Yacht));
    }

    [Fact]
    public void Unknown_cover_type_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(Start, Start.AddDays(10), (CoverType)99));
    }
}
