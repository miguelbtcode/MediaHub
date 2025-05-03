using FluentAssertions;
using MediaHub.Core;
using MediaHub.DependencyInjection;
using MediaHub.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MediaHub.Tests
{
    public class MediatorTests
    {
        [Fact]
        public async Task Send_ShouldResolveHandler_AndReturnResponse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(MediatorTests).Assembly));
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new TestRequest { Value = "Hello World" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be("Hello World Handled");
        }
        
        [Fact]
        public async Task Send_WithNoHandler_ShouldThrowException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(MediatorTests).Assembly));
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new NoHandlerRequest { Value = "Hello World" };
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(request));
        }
        
        [Fact]
        public async Task Send_WithVoidResponse_ShouldReturnUnitValue()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(MediatorTests).Assembly));
            services.AddTransient<IRequestHandler<TestVoidRequest, Unit>, TestVoidRequestHandler>();
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new TestVoidRequest { Value = "Hello World" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be(Unit.Value);
        }

        [Fact]
        public async Task Send_WithISender_ShouldResolveHandler_AndReturnResponse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(MediatorTests).Assembly));
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            
            var provider = services.BuildServiceProvider();
            var sender = provider.GetRequiredService<ISender>();
            var request = new TestRequest { Value = "Hello World" };
            
            // Act
            var response = await sender.Send(request);
            
            // Assert
            response.Should().Be("Hello World Handled");
        }

        [Fact]
        public async Task Publish_WithIPublisher_ShouldInvokeAllHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(MediatorTests).Assembly));
            
            var handlerCallCount = 0;
            var firstMock = new Mock<INotificationHandler<TestNotification>>();
            firstMock.Setup(x => x.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Callback(() => handlerCallCount++)
                .Returns(Task.CompletedTask);
                
            var secondMock = new Mock<INotificationHandler<TestNotification>>();
            secondMock.Setup(x => x.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Callback(() => handlerCallCount++)
                .Returns(Task.CompletedTask);
            
            services.AddTransient<INotificationHandler<TestNotification>>(_ => firstMock.Object);
            services.AddTransient<INotificationHandler<TestNotification>>(_ => secondMock.Object);
            
            var provider = services.BuildServiceProvider();
            var publisher = provider.GetRequiredService<IPublisher>();
            var notification = new TestNotification { Message = "Test" };
            
            // Act
            await publisher.Publish(notification);
            
            // Assert
            handlerCallCount.Should().Be(2);
            firstMock.Verify(x => x.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
            secondMock.Verify(x => x.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Send_WithPipelineBehavior_ShouldExecuteBehavior()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Setup mock behavior
            var behaviorMock = new Mock<IPipelineBehavior<TestRequest, string>>();
            behaviorMock
                .Setup(x => x.Handle(It.IsAny<TestRequest>(), It.IsAny<RequestHandlerDelegate<string>>(), It.IsAny<CancellationToken>()))
                .Returns<TestRequest, RequestHandlerDelegate<string>, CancellationToken>((req, next, ct) => next());
            
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(MediatorTests).Assembly));
            
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            services.AddTransient<IPipelineBehavior<TestRequest, string>>(_ => behaviorMock.Object);
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new TestRequest { Value = "Hello World" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be("Hello World Handled");
            behaviorMock.Verify(x => x.Handle(request, It.IsAny<RequestHandlerDelegate<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Send_WithLoggingPipelineBehavior_ShouldExecuteBehavior()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Setup mock behavior
            var loggerMock = new Mock<ILogger<LoggingPipelineBehaviorImpl<TestRequest, string>>>();
            
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(MediatorTests).Assembly));
            
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            services.AddTransient<ILoggingPipelineBehavior<TestRequest, string>, LoggingPipelineBehaviorImpl<TestRequest, string>>();
            services.AddTransient<IPipelineBehavior<TestRequest, string>>(sp => 
                sp.GetRequiredService<ILoggingPipelineBehavior<TestRequest, string>>());
            services.AddSingleton(loggerMock.Object);
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new TestRequest { Value = "Hello World" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be("Hello World Handled");
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Handling")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Send_WithValidationPipelineBehavior_ShouldExecuteBehavior()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Setup mock behavior
            var validatorMock = new Mock<IValidator<TestRequest>>();
            validatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(MediatorTests).Assembly));
            
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            services.AddTransient<IValidator<TestRequest>>(_ => validatorMock.Object);
            services.AddTransient<IValidationPipelineBehavior<TestRequest, string>, ValidationPipelineBehaviorImpl<TestRequest, string>>();
            services.AddTransient<IPipelineBehavior<TestRequest, string>>(sp => 
                sp.GetRequiredService<IValidationPipelineBehavior<TestRequest, string>>());
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new TestRequest { Value = "Hello World" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be("Hello World Handled");
            validatorMock.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // Test request & handler classes
        public class NoHandlerRequest : IRequest<string>
        {
            public string Value { get; set; }
        }

        public class TestRequest : IRequest<string>
        {
            public string Value { get; set; }
        }

        public class TestRequestHandler : IRequestHandler<TestRequest, string>
        {
            public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult($"{request.Value} Handled");
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
                // Do something with request.Value
                return Task.FromResult(Unit.Value);
            }
        }

        public class TestNotification : INotification
        {
            public string Message { get; set; }
        }

        public class LoggingPipelineBehaviorImpl<TRequest, TResponse> : ILoggingPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            private readonly ILogger<LoggingPipelineBehaviorImpl<TRequest, TResponse>> _logger;

            public LoggingPipelineBehaviorImpl(ILogger<LoggingPipelineBehaviorImpl<TRequest, TResponse>> logger)
            {
                _logger = logger;
            }

            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            {
                var requestName = typeof(TRequest).Name;
                
                _logger.LogInformation("Handling {RequestName}", requestName);
                
                var response = await next();
                
                _logger.LogInformation("Handled {RequestName}", requestName);
                
                return response;
            }
        }

        public class ValidationPipelineBehaviorImpl<TRequest, TResponse> : IValidationPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            private readonly IEnumerable<IValidator<TRequest>> _validators;

            public ValidationPipelineBehaviorImpl(IEnumerable<IValidator<TRequest>> validators)
            {
                _validators = validators;
            }

            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            {
                if (_validators.Any())
                {
                    var context = new ValidationContext<TRequest>(request);
                    
                    var validationResults = await Task.WhenAll(
                        _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                    
                    var failures = validationResults
                        .SelectMany(r => r.Errors)
                        .Where(f => f != null)
                        .ToList();
                    
                    if (failures.Count != 0)
                        throw new ValidationException(failures);
                }
                
                return await next();
            }
        }

        // Add validation structures needed for tests
        public class ValidationContext<T>
        {
            public T InstanceToValidate { get; }

            public ValidationContext(T instanceToValidate)
            {
                InstanceToValidate = instanceToValidate;
            }
        }

        public interface IValidator<T>
        {
            Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken = default);
        }

        public class ValidationResult
        {
            public IList<ValidationFailure> Errors { get; } = new List<ValidationFailure>();
        }

        public class ValidationFailure
        {
            public string PropertyName { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class ValidationException : Exception
        {
            public IList<ValidationFailure> Errors { get; }

            public ValidationException(IList<ValidationFailure> errors)
                : base("One or more validation failures have occurred.")
            {
                Errors = errors;
            }
        }
    }
}