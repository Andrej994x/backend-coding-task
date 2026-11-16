using Claims.Domain;

namespace Claims.Services;

/// <summary>Calculates the premium of a cover. Has no state, so it is easy to unit test.</summary>
public class PremiumCalculator
{
    private const decimal BaseDayRate = 1250m;
    private const int FullPriceDays = 30;
    private const int FirstDiscountLastDay = 180;

    public decimal Calculate(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        // Count whole calendar days; the end date is not charged (1 Jan -> 2 Jan is one day).
        var days = (endDate.Date - startDate.Date).Days;
        if (days <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(endDate), "The end date must be at least one day after the start date.");
        }

        var dayRate = BaseDayRate * Multiplier(coverType);
        var (firstDiscount, secondDiscount) = Discounts(coverType);

        // Split the period into price bands, so every day is charged exactly once.
        var fullPriceDayCount = Math.Min(days, FullPriceDays);
        var firstDiscountDayCount = Math.Clamp(days - FullPriceDays, 0, FirstDiscountLastDay - FullPriceDays);
        var secondDiscountDayCount = Math.Max(days - FirstDiscountLastDay, 0);

        return dayRate * (fullPriceDayCount
                          + firstDiscountDayCount * (1 - firstDiscount)
                          + secondDiscountDayCount * (1 - secondDiscount));
    }

    private static decimal Multiplier(CoverType coverType) => coverType switch
    {
        CoverType.Yacht => 1.1m,
        CoverType.PassengerShip => 1.2m,
        CoverType.Tanker => 1.5m,
        CoverType.ContainerShip => 1.3m,
        CoverType.BulkCarrier => 1.3m,
        _ => throw new ArgumentOutOfRangeException(nameof(coverType), coverType, "Unknown cover type.")
    };

    // Discount for days 31-180, and the total discount from day 181.
    // "Additional" means the second discount is added to the first one: 5% + 3% = 8% for Yacht, 2% + 1% = 3% for others.
    private static (decimal FirstDiscount, decimal SecondDiscount) Discounts(CoverType coverType) =>
        coverType == CoverType.Yacht ? (0.05m, 0.08m) : (0.02m, 0.03m);
}
