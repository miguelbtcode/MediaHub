namespace MediaHub.Validation;

/// <summary>
/// Validation context
/// </summary>
/// <typeparam name="T">Type to validate</typeparam>
public class ValidationContext<T>
{
    /// <summary>
    /// Instance to validate
    /// </summary>
    public T Instance { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationContext class
    /// </summary>
    /// <param name="instance">Instance to validate</param>
    public ValidationContext(T instance)
    {
        Instance = instance;
    }
}