using FluentAssertions;
using MediaHub.Core;
using MediaHub.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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
    }
}