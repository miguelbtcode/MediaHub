using MediaHub.Core;
using System.Reflection;
using MediaHub.Contracts;
using Microsoft.Extensions.DependencyInjection;
using MediaHub.Contracts.Pipelines;
using MediaHub.Abstractions;

namespace MediaHub.DependencyInjection
{
    /// <summary>
    /// Extensions for registering MediaHub with DI container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds MediaHub mediator and request handlers 
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assemblies">Assemblies to scan for handlers</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddMediaHub(this IServiceCollection services, params Assembly[] assemblies)
        {
            return services.AddMediaHub(configuration => configuration
            .RegisterServicesFromAssemblies(assemblies)
            .AddGlobalPipelineBehavior(typeof(IValidationPipelineBehavior<,>)));
        }

        /// <summary>
        /// Adds MediaHub mediator and request handlers
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">MediaHub configuration</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddMediaHub(this IServiceCollection services, Action<MediaHubConfiguration> configuration)
        {
            var config = new MediaHubConfiguration(services);
            configuration(config);

            services.AddTransient<IMediator, Mediator>();
            services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
            services.AddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

            return services;
        }

        /// <summary>
        /// Registers a request handler
        /// </summary>
        public static MediaHubConfiguration RegisterRequestHandler<TRequest, TResponse, THandler>(this MediaHubConfiguration config)
            where TRequest : IRequest<TResponse>
            where THandler : class, IRequestHandler<TRequest, TResponse>
        {
            config.AddService<IRequestHandler<TRequest, TResponse>, THandler>();
            return config;
        }

        /// <summary>
        /// Registers a notification handler
        /// </summary>
        public static MediaHubConfiguration RegisterNotificationHandler<TNotification, TNotificationHandler>(this MediaHubConfiguration config)
            where TNotification : INotification
            where TNotificationHandler : class, INotificationHandler<TNotification>
        {
            config.AddService<INotificationHandler<TNotification>, TNotificationHandler>();
            return config;
        }

        /// <summary>
        /// Add a pipeline behavior for requests
        /// </summary>
        public static MediaHubConfiguration AddPipelineBehavior<TBehavior, TRequest, TResponse>(this MediaHubConfiguration config)
            where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            config.AddService<IPipelineBehavior<TRequest, TResponse>, TBehavior>();
            return config;
        }

        /// <summary>
        /// Add a validation pipeline behavior for requests
        /// </summary>
        public static MediaHubConfiguration AddValidationBehavior<TRequest, TResponse>(this MediaHubConfiguration config)
            where TRequest : IRequest<TResponse>
        {
            config.AddService<IValidationPipelineBehavior<TRequest, TResponse>, ValidationPipelineBehaviorBase<TRequest, TResponse>>();
            config.AddServiceFactory<IPipelineBehavior<TRequest, TResponse>>(
                sp => sp.GetRequiredService<IValidationPipelineBehavior<TRequest, TResponse>>());
            return config;
        }

        /// <summary>
        /// Add a logging pipeline behavior for requests
        /// </summary>
        public static MediaHubConfiguration AddLoggingBehavior<TRequest, TResponse>(this MediaHubConfiguration config)
            where TRequest : IRequest<TResponse>
        {
            config.AddServiceFactory<IPipelineBehavior<TRequest, TResponse>>(
                sp => sp.GetRequiredService<ILoggingPipelineBehavior<TRequest, TResponse>>());
            return config;
        }

        /// <summary>
        /// Add a pipeline behavior for notifications
        /// </summary>
        public static MediaHubConfiguration AddNotificationPipelineBehavior<TBehavior, TNotification>(this MediaHubConfiguration config)
            where TBehavior : class, INotificationPipelineBehavior<TNotification>
            where TNotification : INotification
        {
            config.AddService<INotificationPipelineBehavior<TNotification>, TBehavior>();
            return config;
        }
        
        /// <summary>
        /// Add a global pipeline behavior that applies to all request types
        /// </summary>
        public static MediaHubConfiguration AddGlobalPipelineBehavior<TPipelineBehavior>(this MediaHubConfiguration config)
            where TPipelineBehavior : class
        {
            return config.AddGlobalPipelineBehavior(typeof(TPipelineBehavior));
        }

        /// <summary>
        /// Add a global pipeline behavior that applies to all request types
        /// </summary>
        public static MediaHubConfiguration AddGlobalPipelineBehavior(this MediaHubConfiguration config, Type behaviorType)
        {
            if (behaviorType.IsGenericTypeDefinition)
            {
                // Handle open generic types
                config.AddService(typeof(IPipelineBehavior<,>), behaviorType);
            }
            else
            {
                // Handle closed generic types
                foreach (var interfaceType in behaviorType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)))
                {
                    config.AddService(interfaceType, behaviorType);
                }
            }

            return config;
        }

        /// <summary>
        /// Add a global notification pipeline behavior that applies to all notification types
        /// </summary>
        public static MediaHubConfiguration AddGlobalNotificationPipelineBehavior<TPipelineBehavior>(this MediaHubConfiguration config)
            where TPipelineBehavior : class
        {
            return config.AddGlobalNotificationPipelineBehavior(typeof(TPipelineBehavior));
        }

        /// <summary>
        /// Add a global notification pipeline behavior that applies to all notification types
        /// </summary>
        public static MediaHubConfiguration AddGlobalNotificationPipelineBehavior(this MediaHubConfiguration config, Type behaviorType)
        {
            if (behaviorType.IsGenericTypeDefinition)
            {
                // Handle open generic types
                config.AddService(typeof(INotificationPipelineBehavior<>), behaviorType);
            }
            else
            {
                // Handle closed generic types
                foreach (var interfaceType in behaviorType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationPipelineBehavior<>)))
                {
                    config.AddService(interfaceType, behaviorType);
                }
            }

            return config;
        }
    }
}