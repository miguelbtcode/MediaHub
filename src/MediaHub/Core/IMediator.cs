namespace MediaHub.Core;

/// <summary>
/// Mediator interface for encapsulating request/response and publishing interaction patterns
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Asynchronously send a request to a single handler
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task that represents the send operation. Task result contains the handler response</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously send a request to a single handler with no response
    /// </summary>
    /// <param name="request">Request object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task that represents the send operation.</returns>
    Task<Unit> Send(IRequest request, CancellationToken cancellationToken = default);
}