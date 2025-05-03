using FluentAssertions;
using MediaHub.Core;
using MediaHub.DependencyInjection;
using MediaHub.Contracts.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using MediaHub.Contracts;

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
        public void AddMediaHub_ShouldRegisterSenderAndPublisher()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMediaHub(typeof(ServiceCollectionExtensionsTests).Assembly);
            
            // Assert
            var senderDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISender));
            senderDescriptor.Should().NotBeNull();
            senderDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
            
            var publisherDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPublisher));
            publisherDescriptor.Should().NotBeNull();
            publisherDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
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
        public void RegisterNotificationHandler_ShouldRegisterNotificationHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMediaHub(config => config
                .RegisterNotificationHandler<TestNotification, TestNotificationHandler>());
            
            // Assert
            var handlerDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(INotificationHandler<TestNotification>));
            
            handlerDescriptor.Should().NotBeNull();
            handlerDescriptor.ImplementationType.Should().Be(typeof(TestNotificationHandler));
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
        public void AddValidationBehavior_ShouldRegisterValidationBehavior()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(TestRequest).Assembly)
                .AddValidationBehavior<TestRequest, string>());
            
            // Assert
            var validationBehaviorDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(IValidationPipelineBehavior<TestRequest, string>));
            
            var pipelineBehaviorDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(IPipelineBehavior<TestRequest, string>) &&
                s.ImplementationFactory != null);
            
            validationBehaviorDescriptor.Should().NotBeNull();
            validationBehaviorDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
            
            pipelineBehaviorDescriptor.Should().NotBeNull();
            pipelineBehaviorDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
        }
        
        [Fact]
        public void AddLoggingBehavior_ShouldRegisterLoggingBehavior()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<ILoggingPipelineBehavior<TestRequest, string>, TestLoggingPipelineBehavior>();
            
            // Act
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(TestRequest).Assembly)
                .AddLoggingBehavior<TestRequest, string>());
            
            // Assert
            var pipelineBehaviorDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(IPipelineBehavior<TestRequest, string>) &&
                s.ImplementationFactory != null);
            
            pipelineBehaviorDescriptor.Should().NotBeNull();
            pipelineBehaviorDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
        }
        
        [Fact]
        public void AddNotificationPipelineBehavior_ShouldRegisterNotificationBehavior()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(TestRequest).Assembly)
                .AddNotificationPipelineBehavior<TestNotificationPipelineBehavior, TestNotification>());
            
            // Assert
            var behaviorDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(INotificationPipelineBehavior<TestNotification>));
            
            behaviorDescriptor.Should().NotBeNull();
            behaviorDescriptor.ImplementationType.Should().Be(typeof(TestNotificationPipelineBehavior));
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
        
        [Fact]
        public void AddGlobalNotificationPipelineBehavior_ShouldRegisterForAllImplementedInterfaces()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(TestRequest).Assembly)
                .AddGlobalNotificationPipelineBehavior<GlobalTestNotificationPipelineBehavior>());
            
            // Assert
            var behaviorDescriptor = services.FirstOrDefault(s => 
                s.ServiceType == typeof(INotificationPipelineBehavior<TestNotification>));
            
            behaviorDescriptor.Should().NotBeNull();
            behaviorDescriptor.ImplementationType.Should().Be(typeof(GlobalTestNotificationPipelineBehavior));
        }
        
        // Test classes
        public class TestRequest : IRequest<string>
        {
            public string Value { get; set; } = string.Empty;
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
            public string Value { get; set; } = string.Empty;
        }
        
        public class TestVoidRequestHandler : IRequestHandler<TestVoidRequest>
        {
            public Task<Unit> Handle(TestVoidRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(Unit.Value);
            }
        }
        
        public class TestNotification : INotification
        {
            public string Message { get; set; } = string.Empty;
        }
        
        public class TestNotificationHandler : INotificationHandler<TestNotification>
        {
            public Task Handle(TestNotification notification, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
        
        public class TestPipelineBehavior : IPipelineBehavior<TestRequest, string>
        {
            public Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                return next();
            }
        }
        
        public class TestValidationPipelineBehavior : IValidationPipelineBehavior<TestRequest, string>
        {
            public Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                return next();
            }
        }
        
        public class TestLoggingPipelineBehavior : ILoggingPipelineBehavior<TestRequest, string>
        {
            public Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                return next();
            }
        }
        
        public class TestNotificationPipelineBehavior : INotificationPipelineBehavior<TestNotification>
        {
            public Task Handle(TestNotification notification, NotificationHandlerDelegate next, CancellationToken cancellationToken)
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
        
        public class GlobalTestNotificationPipelineBehavior : INotificationPipelineBehavior<TestNotification>
        {
            public Task Handle(TestNotification notification, NotificationHandlerDelegate next, CancellationToken cancellationToken)
            {
                return next();
            }
        }
    }
}