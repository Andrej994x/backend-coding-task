namespace Claims.Contracts;

public static class DateTimeExtensions
{
    // A date without an offset (for example "2027-01-01") is treated as UTC, not as server local time.
    public static DateTime AsUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => value
    };
}
