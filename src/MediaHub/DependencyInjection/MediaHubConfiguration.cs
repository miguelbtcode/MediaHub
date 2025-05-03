using MediaHub.Contracts;
using MediaHub.Contracts.Pipelines;
using MediaHub.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MediaHub.DependencyInjection
{
    /// <summary>
    /// MediaHub configuration for advanced options
    /// </summary>
    public class MediaHubConfiguration
    {
        private readonly IServiceCollection _services;

        public MediaHubConfiguration(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// Adds a service to the configuration
        /// </summary>
        /// <typeparam name="TService">Service type</typeparam>
        /// <typeparam name="TImplementation">Implementation type</typeparam>
        /// <returns>MediaHub configuration</returns>
        public MediaHubConfiguration AddService<TService, TImplementation>()
            where TImplementation : class, TService
            where TService : class
        {
            _services.AddTransient<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adds a service to the configuration
        /// </summary>
        /// <param name="serviceType">Service type</param>
        /// <param name="implementationType">Implementation type</param>
        /// <returns>MediaHub configuration</returns>
        public MediaHubConfiguration AddService(Type serviceType, Type implementationType)
        {
            _services.AddTransient(serviceType, implementationType);
            return this;
        }

        /// <summary>
        /// Adds a service with a factory to the configuration
        /// </summary>
        /// <typeparam name="TService">Service type</typeparam>
        /// <param name="factory">Factory function</param>
        /// <returns>MediaHub configuration</returns>
        public MediaHubConfiguration AddServiceFactory<TService>(Func<IServiceProvider, TService> factory)
            where TService : class
        {
            _services.AddTransient(factory);
            return this;
        }

        /// <summary>
        /// Register services from assemblies
        /// </summary>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>MediaHub configuration</returns>
        public MediaHubConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
        {
            RegisterHandlers(assemblies);
            
            return this;
        }

        /// <summary>
        /// Adds a pipeline behavior to the request processing pipeline
        /// </summary>
        /// <typeparam name="TPipelineBehavior">Pipeline behavior type</typeparam>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <returns>MediaHub configuration</returns>
        public MediaHubConfiguration AddPipelineBehavior<TPipelineBehavior, TRequest, TResponse>()
            where TPipelineBehavior : class, IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            _services.AddTransient<IPipelineBehavior<TRequest, TResponse>, TPipelineBehavior>();
            return this;
        }

        /// <summary>
        /// Adds a pipeline behavior that applies to all requests
        /// </summary>
        /// <param name="behaviorType">Concrete pipeline behavior type</param>
        /// <param name="interfaceType">Interface pipeline behavior type</param>
        /// <returns>MediaHub configuration</returns>
        public MediaHubConfiguration AddPipelineBehavior(Type behaviorType, Type interfaceType)
        {
            _services.AddTransient(interfaceType, behaviorType);
            return this;
        }

        /// <summary>
        /// Adds a global pipeline behavior that applies to all requests
        /// </summary>
        /// <typeparam name="TPipelineBehavior">Pipeline behavior type</typeparam>
        /// <returns>MediaHub configuration</returns>
        public MediaHubConfiguration AddGlobalPipelineBehavior(Type behaviorType)
        {
            if (behaviorType.IsGenericTypeDefinition)
            {
                // Handle open generic types
                _services.AddTransient(typeof(IPipelineBehavior<,>), behaviorType);
            }
            else
            {
                // Handle closed generic types
                foreach (var interfaceType in behaviorType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)))
                {
                    _services.AddTransient(interfaceType, behaviorType);
                }
            }

            return this;
        }

        public MediaHubConfiguration AddGlobalPipelineBehavior<TPipelineBehavior>()
            where TPipelineBehavior : class
        {
            return AddGlobalPipelineBehavior(typeof(TPipelineBehavior));
        }

        private void RegisterHandlers(IEnumerable<Assembly> assemblies)
        {
            var openRequestHandlerTypes = new[] {
                typeof(IRequestHandler<,>),
                typeof(IRequestHandler<>)
            };

            foreach (var assembly in assemblies)
            {
                var requests = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass &&
                                t.GetInterfaces().Any(i => i.IsGenericType &&
                                    (i.GetGenericTypeDefinition() == typeof(IRequest<>) ||
                                     i.GetGenericTypeDefinition() == typeof(IRequest))))
                    .ToList();

                var handlers = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass &&
                                t.GetInterfaces().Any(i => i.IsGenericType &&
                                    openRequestHandlerTypes.Contains(i.GetGenericTypeDefinition())))
                    .ToList();

                foreach (var handler in handlers)
                {
                    foreach (var handlerInterface in handler.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                               openRequestHandlerTypes.Contains(i.GetGenericTypeDefinition())))
                    {
                        _services.AddTransient(handlerInterface, handler);
                    }
                }
            }
        }
    }
}