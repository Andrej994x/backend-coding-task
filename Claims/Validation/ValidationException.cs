namespace Claims.Validation;

/// <summary>Thrown when a request breaks a business rule. It is returned to the client as HTTP 400.</summary>
public class ValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public static void ThrowIfAny(IDictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
