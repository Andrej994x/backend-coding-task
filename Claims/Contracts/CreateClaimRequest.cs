using System.ComponentModel.DataAnnotations;
using Claims.Domain;

namespace Claims.Contracts;

/// <summary>Body of POST /Claims. Id is not part of the request because the server generates it.</summary>
public class CreateClaimRequest
{
    [Required]
    public string CoverId { get; init; } = null!;

    // Value types are nullable so that a missing field is rejected instead of silently getting a default value.
    [Required]
    public DateTime? Created { get; init; }

    [Required]
    public string Name { get; init; } = null!;

    [Required]
    public ClaimType? Type { get; init; }

    [Required]
    public decimal? DamageCost { get; init; }

    public Claim ToClaim() => new()
    {
        CoverId = CoverId,
        Created = Created!.Value.AsUtc(),
        Name = Name,
        Type = Type!.Value,
        DamageCost = DamageCost!.Value
    };
}
