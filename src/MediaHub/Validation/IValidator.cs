using System.ComponentModel.DataAnnotations;

namespace MediaHub.Validation
{
    /// <summary>
    /// Interface for validators
    /// </summary>
    /// <typeparam name="T">Type to validate</typeparam>
    public interface IValidator<T>
    {
        /// <summary>
        /// Validates the specified instance
        /// </summary>
        /// <param name="context">Validation context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken);
    }
}