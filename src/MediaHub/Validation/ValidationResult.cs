namespace MediaHub.Validation;

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Validation errors
    /// </summary>
    public IEnumerable<ValidationFailure> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationResult class
    /// </summary>
    /// <param name="failures">Validation failures</param>
    public ValidationResult(IEnumerable<ValidationFailure> failures)
    {
        Errors = failures;
    }
}