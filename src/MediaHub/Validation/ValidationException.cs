namespace MediaHub.Validation;

/// <summary>
/// Validation exception
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Validation failures
    /// </summary>
    public IEnumerable<ValidationFailure> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationException class
    /// </summary>
    /// <param name="failures">Validation failures</param>
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation failures have occurred.")
    {
        Errors = failures;
    }
}