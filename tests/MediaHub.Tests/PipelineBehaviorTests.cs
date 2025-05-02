using FluentAssertions;
using MediaHub.Core;
using MediaHub.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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
    }
}