using System.ComponentModel.DataAnnotations;
using Claims.Domain;

namespace Claims.Contracts;

/// <summary>Body of POST /Covers. Id and Premium are not part of the request because the server sets them.</summary>
public class CreateCoverRequest
{
    // Value types are nullable so that a missing field is rejected instead of silently getting a default value.
    [Required]
    public DateTime? StartDate { get; init; }

    [Required]
    public DateTime? EndDate { get; init; }

    [Required]
    public CoverType? Type { get; init; }

    public Cover ToCover() => new()
    {
        StartDate = StartDate!.Value.AsUtc(),
        EndDate = EndDate!.Value.AsUtc(),
        Type = Type!.Value
    };
}
