using MediaHub.Pipelines;
using MediaHub.Validation;

namespace MediaHub.Abstractions;

/// <summary>
/// Abstract base implementation of IValidationPipelineBehavior that provides standard validation handling
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public abstract class ValidationPipelineBehaviorBase<TRequest, TResponse> : IValidationPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    protected ValidationPipelineBehaviorBase(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
    }
    
    public virtual async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        await ValidateRequest(request, cancellationToken);
        
        return await next();
    }

    /// <summary>
    /// Validates the request using all registered validators
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the validation operation</returns>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    protected virtual async Task ValidateRequest(TRequest request, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Count != 0)
        {
            await OnValidationFailure(request, failures);
            throw new ValidationException(failures);
        }
    }

    /// <summary>
    /// Called when validation fails, before the exception is thrown
    /// Can be overridden to add custom behavior like logging
    /// </summary>
    /// <param name="request">The request that failed validation</param>
    /// <param name="failures">The validation failures</param>
    /// <returns>Task representing the operation</returns>
    protected virtual Task OnValidationFailure(TRequest request, IList<ValidationFailure> failures)
    {
        return Task.CompletedTask;
    }
}