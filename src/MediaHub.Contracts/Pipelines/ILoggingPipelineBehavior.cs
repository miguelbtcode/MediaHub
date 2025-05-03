namespace MediaHub.Contracts.Pipelines;

/// <summary>
/// Contract for a behavior that handles logging and performance tracking
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface ILoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
}