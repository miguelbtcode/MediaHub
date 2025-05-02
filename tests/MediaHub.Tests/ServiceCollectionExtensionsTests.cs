using System.Linq;
using System.Reflection;
using FluentAssertions;
using MediaHub.Core;
using MediaHub.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediaHub.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddMediaHub_ShouldRegisterMediator()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMediaHub(typeof(ServiceCollectionExtensionsTests).Assembly);
            
            // Assert
            var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
            serviceDescriptor.Should().NotBeNull();
            serviceDescriptor.ImplementationType.Should().Be(typeof(Mediator));
            serviceDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
        }
        
        [Fact]
        public void AddMediaHub_ShouldRegisterHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(TestRequest).Assembly));
            
            // Assert
            var handlerDescriptor = services.FirstOrDefault(s => 
                s.ServiceType.IsGenericType && 
                s.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
            
            handlerDescriptor.Should().NotBeNull();
            handlerDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
        }
        
        [Fact]
        public void AddPipelineBehavior_ShouldRegisterSpecificBehavior()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(TestRequest).Assembly)
                .AddPipelineBehavior<TestPipelineBehavior, TestRequest, string>());
            
            // Assert
            var behaviorDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(IPipelineBehavior<TestRequest, string>));
            
            behaviorDescriptor.Should().NotBeNull();
            behaviorDescriptor.ImplementationType.Should().Be(typeof(TestPipelineBehavior));
            behaviorDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
        }
        
        [Fact]
        public void AddGlobalPipelineBehavior_ShouldRegisterForAllImplementedInterfaces()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(TestRequest).Assembly)
                .AddGlobalPipelineBehavior<GlobalTestPipelineBehavior>());
            
            // Assert
            var firstBehaviorDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(IPipelineBehavior<TestRequest, string>));
            
            var secondBehaviorDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(IPipelineBehavior<TestVoidRequest, Unit>));
            
            firstBehaviorDescriptor.Should().NotBeNull();
            firstBehaviorDescriptor.ImplementationType.Should().Be(typeof(GlobalTestPipelineBehavior));
            
            secondBehaviorDescriptor.Should().NotBeNull();
            secondBehaviorDescriptor.ImplementationType.Should().Be(typeof(GlobalTestPipelineBehavior));
        }
        
        // Test classes
        public class TestRequest : IRequest<string>
        {
            public string Value { get; set; }
        }
        
        public class TestRequestHandler : IRequestHandler<TestRequest, string>
        {
            public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(request.Value);
            }
        }
        
        public class TestVoidRequest : IRequest
        {
            public string Value { get; set; }
        }
        
        public class TestVoidRequestHandler : IRequestHandler<TestVoidRequest>
        {
            public Task<Unit> Handle(TestVoidRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(Unit.Value);
            }
        }
        
        public class TestPipelineBehavior : IPipelineBehavior<TestRequest, string>
        {
            public Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                return next();
            }
        }
        
        public class GlobalTestPipelineBehavior : 
            IPipelineBehavior<TestRequest, string>,
            IPipelineBehavior<TestVoidRequest, Unit>
        {
            public Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                return next();
            }
            
            public Task<Unit> Handle(TestVoidRequest request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
            {
                return next();
            }
        }
    }
}