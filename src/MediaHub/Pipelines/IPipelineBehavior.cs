namespace MediaHub.Pipelines;

 /// <summary>
/// Pipeline behavior for request processing
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Pipeline handler for requests
    /// </summary>
    /// <param name="request">Request instance</param>
    /// <param name="next">Next delegate in pipeline</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing the request handling with response</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
/// Delegate for the next request handler in the pipeline
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <returns>Task containing the response</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();