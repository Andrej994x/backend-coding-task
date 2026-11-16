namespace Claims.Tests;

/// <summary>Returns a fixed "now", so date rules are tested in a deterministic way.</summary>
public class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
