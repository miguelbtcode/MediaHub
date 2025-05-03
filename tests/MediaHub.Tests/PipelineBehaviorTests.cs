using FluentAssertions;
using MediaHub.Core;
using MediaHub.DependencyInjection;
using MediaHub.Contracts.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using MediaHub.Contracts;

namespace MediaHub.Tests
{
    public class PipelineBehaviorTests
    {
        [Fact]
        public async Task Send_WithBehavior_ShouldExecuteBehaviorBeforeAndAfterHandler()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(PipelineBehaviorTests).Assembly));
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            services.AddTransient<IPipelineBehavior<TestRequest, string>, TestPipelineBehavior>();
            
            // Store execution order in a singleton for verification
            services.AddSingleton(executionOrder);
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new TestRequest { Value = "Test" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be("Test Handled");
            executionOrder.Should().HaveCount(3);
            executionOrder[0].Should().Be("Before Handler");
            executionOrder[1].Should().Be("Handler TestRequest");
            executionOrder[2].Should().Be("After Handler");
        }
        
        [Fact]
        public async Task Send_WithMultipleBehaviors_ShouldExecuteInCorrectOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(PipelineBehaviorTests).Assembly));
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            services.AddTransient<IPipelineBehavior<TestRequest, string>, FirstPipelineBehavior>();
            services.AddTransient<IPipelineBehavior<TestRequest, string>, SecondPipelineBehavior>();
            
            // Store execution order in a singleton for verification
            services.AddSingleton(executionOrder);
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new TestRequest { Value = "Test" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be("Test Handled");
            executionOrder.Should().HaveCount(5);
            executionOrder[0].Should().Be("First Before");
            executionOrder[1].Should().Be("Second Before");
            executionOrder[2].Should().Be("Handler TestRequest");
            executionOrder[3].Should().Be("Second After");
            executionOrder[4].Should().Be("First After");
        }
        
        [Fact]
        public async Task Send_WithGlobalBehavior_ShouldApplyToAllRequests()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            var services = new ServiceCollection();
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(PipelineBehaviorTests).Assembly)
                .AddGlobalPipelineBehavior<GlobalPipelineBehavior>());
            
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            services.AddTransient<IRequestHandler<AnotherRequest, int>, AnotherRequestHandler>();
            
            // Store execution order in a singleton for verification
            services.AddSingleton(executionOrder);
            services.AddSingleton<GlobalPipelineBehavior>();
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            
            // Act
            var stringResponse = await mediator.Send(new TestRequest { Value = "Test" });
            var intResponse = await mediator.Send(new AnotherRequest { Value = 5 });
            
            // Assert
            stringResponse.Should().Be("Test Handled");
            intResponse.Should().Be(10);
            
            executionOrder.Should().HaveCount(4);
            executionOrder[0].Should().Be("Global Before TestRequest");
            executionOrder[1].Should().Be("Handler TestRequest");
            executionOrder[2].Should().Be("Global Before AnotherRequest");
            executionOrder[3].Should().Be("Handler AnotherRequest");
        }

        [Fact]
        public async Task Send_WithLogingPipelineBehavior_ShouldExecuteBehavior()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(PipelineBehaviorTests).Assembly));
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            services.AddTransient<ILoggingPipelineBehavior<TestRequest, string>, TestLoggingPipelineBehavior>();
            services.AddTransient<IPipelineBehavior<TestRequest, string>>(sp => 
                sp.GetRequiredService<ILoggingPipelineBehavior<TestRequest, string>>());
            
            // Store execution order in a singleton for verification
            services.AddSingleton(executionOrder);
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new TestRequest { Value = "Test" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be("Test Handled");
            executionOrder.Should().HaveCount(3);
            executionOrder[0].Should().Be("Logging Before TestRequest");
            executionOrder[1].Should().Be("Handler TestRequest");
            executionOrder[2].Should().Be("Logging After TestRequest");
        }

        [Fact]
        public async Task Send_WithValidationPipelineBehavior_ShouldExecuteBehavior()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(PipelineBehaviorTests).Assembly));
            services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            services.AddTransient<IValidationPipelineBehavior<TestRequest, string>, TestValidationPipelineBehavior>();
            services.AddTransient<IPipelineBehavior<TestRequest, string>>(sp => 
                sp.GetRequiredService<IValidationPipelineBehavior<TestRequest, string>>());
            
            // Store execution order in a singleton for verification
            services.AddSingleton(executionOrder);
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var request = new TestRequest { Value = "Test" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be("Test Handled");
            executionOrder.Should().HaveCount(3);
            executionOrder[0].Should().Be("Validation Before TestRequest");
            executionOrder[1].Should().Be("Handler TestRequest");
            executionOrder[2].Should().Be("Validation After TestRequest");
        }

        [Fact]
        public async Task Publish_WithNotificationPipelineBehavior_ShouldExecuteBehavior()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            var services = new ServiceCollection();
            services.AddMediaHub(config => config.RegisterServicesFromAssemblies(typeof(PipelineBehaviorTests).Assembly));
            services.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler>();
            services.AddTransient<INotificationPipelineBehavior<TestNotification>, TestNotificationPipelineBehavior>();
            
            // Store execution order in a singleton for verification
            services.AddSingleton(executionOrder);
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IPublisher>();
            var notification = new TestNotification { Message = "Test Notification" };
            
            // Act
            await mediator.Publish(notification);
            
            // Assert
            executionOrder.Should().HaveCount(3);
            executionOrder[0].Should().Be("Notification Behavior Before");
            executionOrder[1].Should().Be("Notification Handler");
            executionOrder[2].Should().Be("Notification Behavior After");
        }
        
        // Test request & handler classes
        public class TestRequest : IRequest<string>
        {
            public string Value { get; set; }
        }
        
        public class TestRequestHandler : IRequestHandler<TestRequest, string>
        {
            private readonly List<string> _executionOrder;
            
            public TestRequestHandler(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Handler TestRequest");
                return Task.FromResult($"{request.Value} Handled");
            }
        }
        
        public class AnotherRequest : IRequest<int>
        {
            public int Value { get; set; }
        }
        
        public class AnotherRequestHandler : IRequestHandler<AnotherRequest, int>
        {
            private readonly List<string> _executionOrder;
            
            public AnotherRequestHandler(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public Task<int> Handle(AnotherRequest request, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Handler AnotherRequest");
                return Task.FromResult(request.Value * 2);
            }
        }

        public class TestNotification : INotification
        {
            public string Message { get; set; }
        }

        public class TestNotificationHandler : INotificationHandler<TestNotification>
        {
            private readonly List<string> _executionOrder;
            
            public TestNotificationHandler(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public Task Handle(TestNotification notification, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Notification Handler");
                return Task.CompletedTask;
            }
        }
        
        // Test pipeline behaviors
        public class TestPipelineBehavior : IPipelineBehavior<TestRequest, string>
        {
            private readonly List<string> _executionOrder;
            
            public TestPipelineBehavior(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public async Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Before Handler");
                
                var response = await next();
                
                _executionOrder.Add("After Handler");
                
                return response;
            }
        }
        
        public class FirstPipelineBehavior : IPipelineBehavior<TestRequest, string>
        {
            private readonly List<string> _executionOrder;
            
            public FirstPipelineBehavior(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public async Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                _executionOrder.Add("First Before");
                
                var response = await next();
                
                _executionOrder.Add("First After");
                
                return response;
            }
        }
        
        public class SecondPipelineBehavior : IPipelineBehavior<TestRequest, string>
        {
            private readonly List<string> _executionOrder;
            
            public SecondPipelineBehavior(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public async Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Second Before");
                
                var response = await next();
                
                _executionOrder.Add("Second After");
                
                return response;
            }
        }
        
        public class GlobalPipelineBehavior : 
            IPipelineBehavior<TestRequest, string>,
            IPipelineBehavior<AnotherRequest, int>
        {
            private readonly List<string> _executionOrder;
            
            public GlobalPipelineBehavior(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public async Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Global Before TestRequest");
                return await next();
            }
            
            public async Task<int> Handle(AnotherRequest request, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Global Before AnotherRequest");
                return await next();
            }
        }

        // Specialized pipeline behaviors
        public class TestLoggingPipelineBehavior : ILoggingPipelineBehavior<TestRequest, string>
        {
            private readonly List<string> _executionOrder;
            
            public TestLoggingPipelineBehavior(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public async Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Logging Before TestRequest");
                
                var response = await next();
                
                _executionOrder.Add("Logging After TestRequest");
                
                return response;
            }
        }

        public class TestValidationPipelineBehavior : IValidationPipelineBehavior<TestRequest, string>
        {
            private readonly List<string> _executionOrder;
            
            public TestValidationPipelineBehavior(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public async Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Validation Before TestRequest");
                
                var response = await next();
                
                _executionOrder.Add("Validation After TestRequest");
                
                return response;
            }
        }

        public class TestNotificationPipelineBehavior : INotificationPipelineBehavior<TestNotification>
        {
            private readonly List<string> _executionOrder;
            
            public TestNotificationPipelineBehavior(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }
            
            public async Task Handle(TestNotification notification, NotificationHandlerDelegate next, CancellationToken cancellationToken)
            {
                _executionOrder.Add("Notification Behavior Before");
                
                await next();
                
                _executionOrder.Add("Notification Behavior After");
            }
        }
    }
}