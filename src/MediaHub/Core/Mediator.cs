using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace MediaHub.Core
{
    /// <summary>
    /// Default mediator implementation
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<Type, object> _requestHandlerWrappers = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Mediator"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving handlers and behaviors</param>
        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Sends a request to a single handler
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="request">Request object</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task that represents the send operation. Task result contains the handler response</returns>
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var wrapper = (RequestHandlerWrapper<TResponse>)_requestHandlerWrappers.GetOrAdd(
                requestType,
                t => Activator.CreateInstance(typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse)))
                ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}")
            );

            return wrapper.Handle(request, _serviceProvider, cancellationToken);
        }

        /// <summary>
        /// Asynchronously send a request to a single handler with no response
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task that represents the send operation.</returns>
        public Task<Unit> Send(IRequest request, CancellationToken cancellationToken = default)
        {
            return Send<Unit>(request, cancellationToken);
        }

        private abstract class RequestHandlerWrapper<TResponse>
        {
            public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
        }

        private class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
            where TRequest : IRequest<TResponse>
        {
            public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
            {
                // Verificar explícitamente si existe un manejador antes de construir el pipeline
                var handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TRequest), typeof(TResponse));
                var handler = serviceProvider.GetService(handlerType);
                
                if (handler == null)
                    throw new InvalidOperationException($"No handler found for request type {typeof(TRequest).FullName}");
                
                Task<TResponse> Handler() => GetHandler<TRequest, TResponse>(serviceProvider, (TRequest)request).Handle((TRequest)request, cancellationToken);

                return serviceProvider
                    .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                    .Reverse()
                    .Aggregate((RequestHandlerDelegate<TResponse>)Handler,
                            (next, pipeline) => () => pipeline.Handle((TRequest)request, next, cancellationToken))();
            }

            private static IRequestHandler<TReq, TRes> GetHandler<TReq, TRes>(IServiceProvider serviceProvider, TReq request)
                where TReq : IRequest<TRes>
            {
                var handler = serviceProvider.GetService<IRequestHandler<TReq, TRes>>();

                if (handler == null)
                    throw new InvalidOperationException($"No handler found for request type {typeof(TReq).FullName}");

                return handler;
            }
        }
    }
}