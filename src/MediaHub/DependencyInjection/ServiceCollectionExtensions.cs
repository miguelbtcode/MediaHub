using MediaHub.Behaviors;
using MediaHub.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

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
            .AddGlobalPipelineBehavior(typeof(ValidationBehavior<,>)));
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

            return services;
        }
    }
}