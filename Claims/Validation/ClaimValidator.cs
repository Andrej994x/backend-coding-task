using Claims.Domain;

namespace Claims.Validation;

/// <summary>Business rules for a new claim. Pure function, so it is easy to unit test.</summary>
public static class ClaimValidator
{
    public const decimal MaxDamageCost = 100_000m;

    public static Dictionary<string, string[]> Validate(Claim claim, Cover? cover)
    {
        var errors = new Dictionary<string, string[]>();

        // Exactly 100,000 is allowed, only higher values are rejected.
        if (claim.DamageCost > MaxDamageCost)
        {
            errors[nameof(Claim.DamageCost)] = [$"DamageCost cannot exceed {MaxDamageCost:N0}."];
        }

        if (!Enum.IsDefined(claim.Type))
        {
            errors[nameof(Claim.Type)] = ["Unknown claim type."];
        }

        if (cover is null)
        {
            errors[nameof(Claim.CoverId)] = ["The related cover does not exist."];
        }
        // Compare calendar dates only. The first and the last day of the cover are both allowed.
        else if (claim.Created.Date < cover.StartDate.Date || claim.Created.Date > cover.EndDate.Date)
        {
            errors[nameof(Claim.Created)] = ["Created date must be within the period of the related cover."];
        }

        return errors;
    }
}
