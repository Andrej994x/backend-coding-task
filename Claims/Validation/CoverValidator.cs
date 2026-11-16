using Claims.Domain;

namespace Claims.Validation;

/// <summary>Business rules for a cover period. Used for new covers and for premium quotes.</summary>
public class CoverValidator(TimeProvider timeProvider)
{
    public Dictionary<string, string[]> Validate(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        var errors = new Dictionary<string, string[]>();

        // Dates are UTC and compared as calendar dates, so a cover may start today.
        var today = timeProvider.GetUtcNow().UtcDateTime.Date;
        var start = startDate.Date;
        var end = endDate.Date;

        if (start < today)
        {
            errors[nameof(Cover.StartDate)] = ["StartDate cannot be in the past."];
        }

        if (end <= start)
        {
            errors[nameof(Cover.EndDate)] = ["EndDate must be later than StartDate."];
        }
        else if (end > LatestEndDate(start))
        {
            errors[nameof(Cover.EndDate)] = ["The insurance period cannot exceed one year."];
        }

        if (!Enum.IsDefined(coverType))
        {
            errors[nameof(Cover.Type)] = ["Unknown cover type."];
        }

        return errors;
    }

    // Use a calendar year (AddYears), so leap years are handled. A cover from 29 February may end on 28 February.
    private static DateTime LatestEndDate(DateTime start) =>
        start.Year < DateTime.MaxValue.Year ? start.AddYears(1) : DateTime.MaxValue;
}
