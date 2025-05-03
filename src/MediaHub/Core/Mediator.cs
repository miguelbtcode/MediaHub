using MediaHub.Contracts;
using MediaHub.Contracts.Pipelines;
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
        private static readonly ConcurrentDictionary<Type, object> _notificationHandlerWrappers = new();

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
        public Task<Unit> Send(IRequest request, CancellationToken cancellationToken = default)
        {
            return Send<Unit>(request, cancellationToken);
        }

        /// <summary>
        /// Publish a notification to multiple handlers
        /// </summary>
        public Task Publish(INotification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var notificationType = notification.GetType();
            var wrapper = (NotificationHandlerWrapper)_notificationHandlerWrappers.GetOrAdd(
                notificationType,
                t => Activator.CreateInstance(typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType))
                ?? throw new InvalidOperationException($"Could not create notification wrapper for {notificationType}")
            );

            return wrapper.Handle(notification, _serviceProvider, cancellationToken);
        }

        /// <summary>
        /// Publish a notification to multiple handlers
        /// </summary>
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) 
            where TNotification : INotification
        {
            return Publish((INotification)notification, cancellationToken);
        }

        private abstract class NotificationHandlerWrapper
        {
            public abstract Task Handle(INotification notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
        }

        private class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
            where TNotification : INotification
        {
            public override async Task Handle(INotification notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
            {
                // Obtener todos los comportamientos de pipeline
                List<INotificationPipelineBehavior<TNotification>> pipelineBehaviors = 
                    serviceProvider.GetServices<INotificationPipelineBehavior<TNotification>>().ToList();
                
                // Si no hay comportamientos, ejecutar directamente los manejadores
                if (pipelineBehaviors.Count == 0)
                {
                    await ExecuteHandlers((TNotification)notification, serviceProvider, cancellationToken);
                    return;
                }
                
                // Crear un delegado inicial que apunte a ExecuteHandlers
                NotificationHandlerDelegate currentDelegate = () => 
                    ExecuteHandlers((TNotification)notification, serviceProvider, cancellationToken);
                
                // Construir la cadena de comportamientos en orden inverso
                for (int i = pipelineBehaviors.Count - 1; i >= 0; i--)
                {
                    var behavior = pipelineBehaviors[i];
                    var nextDelegate = currentDelegate;
                    currentDelegate = () => behavior.Handle((TNotification)notification, nextDelegate, cancellationToken);
                }
                
                // Ejecutar el pipeline
                await currentDelegate();
            }
            
            private static async Task ExecuteHandlers(TNotification notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
            {
                var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();
                var tasks = handlers.Select(handler => handler.Handle(notification, cancellationToken));
                await Task.WhenAll(tasks);
            }
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