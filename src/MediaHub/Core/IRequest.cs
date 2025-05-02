namespace MediaHub.Core;

/// <summary>
/// Interface for defining a request with a response
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IRequest<out TResponse> : IBaseRequest { }

/// <summary>
/// Marker interface for requests that don't return a value
/// </summary>
public interface IRequest : IRequest<Unit> { }