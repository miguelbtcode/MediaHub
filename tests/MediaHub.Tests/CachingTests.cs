using FluentAssertions;
using MediaHub.Behaviors;
using MediaHub.Caching;
using MediaHub.Core;
using MediaHub.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MediaHub.Tests
{
    public class CachingTests
    {
        [Fact]
        public async Task Send_WithCacheableBehavior_ShouldCacheResponse()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add memory cache
            services.AddMemoryCache();
            
            // Add logging (mock)
            var loggerMock = new Mock<ILogger<CachingBehavior<TestCacheableRequest, string>>>();
            services.AddSingleton(loggerMock.Object);
            
            // Crear una instancia del handler directamente
            var testHandler = new TestCacheableRequestHandler();
            
            // Contador de llamadas
            int handlerCallCount = 0;
            testHandler.CallCountCallback = () => handlerCallCount++;
            
            // Register MediaHub with caching behavior
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(CachingTests).Assembly));
            
            // Registrar el handler como singleton directamente
            services.AddSingleton<IRequestHandler<TestCacheableRequest, string>>(testHandler);
            services.AddTransient<IPipelineBehavior<TestCacheableRequest, string>, CachingBehavior<TestCacheableRequest, string>>();
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            
            var request = new TestCacheableRequest { Id = 123 };
            
            // Act
            var firstResponse = await mediator.Send(request);
            var secondResponse = await mediator.Send(request);
            
            // Create a different request with same cache key to test cache hits
            var requestWithSameCacheKey = new TestCacheableRequest { Id = 123 };
            var thirdResponse = await mediator.Send(requestWithSameCacheKey);
            
            // Assert
            firstResponse.Should().Be("Response for 123");
            secondResponse.Should().Be("Response for 123");
            thirdResponse.Should().Be("Response for 123");
            
            // Verificar que el handler fue llamado solo una vez
            handlerCallCount.Should().Be(1);
            
            // Verify logging
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Returning cached response")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(2));
        }
        
        [Fact]
        public async Task Send_WithDifferentCacheKeys_ShouldNotUseCachedResponse()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add memory cache
            services.AddMemoryCache();
            
            // Add logging (mock)
            var loggerMock = new Mock<ILogger<CachingBehavior<TestCacheableRequest, string>>>();
            services.AddSingleton(loggerMock.Object);

            // Crear una instancia del handler directamente
            var testHandler = new TestCacheableRequestHandler();

            // Contador de llamadas
            int handlerCallCount = 0;
            testHandler.CallCountCallback = () => handlerCallCount++;
            
            // Register MediaHub with caching behavior
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(CachingTests).Assembly));
            
            services.AddSingleton<IRequestHandler<TestCacheableRequest, string>>(testHandler);
            services.AddTransient<IPipelineBehavior<TestCacheableRequest, string>, CachingBehavior<TestCacheableRequest, string>>();
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            
            // Act
            var firstResponse = await mediator.Send(new TestCacheableRequest { Id = 123 });
            var secondResponse = await mediator.Send(new TestCacheableRequest { Id = 456 });
            
            // Assert
            firstResponse.Should().Be("Response for 123");
            secondResponse.Should().Be("Response for 456");
            
            // Verify handler was called twice (different cache keys)
            handlerCallCount.Should().Be(2);
        }
        
        [Fact]
        public async Task Send_WithEmptyCacheKey_ShouldNotCache()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add memory cache
            services.AddMemoryCache();
            
            // Add logging (mock)
            var loggerMock = new Mock<ILogger<CachingBehavior<TestCacheableWithEmptyKeyRequest, string>>>();
            services.AddSingleton(loggerMock.Object);
            
            // Crear una instancia del handler directamente
            var testHandler = new TestCacheableWithEmptyKeyRequestHandler();
            
            // Contador de llamadas
            int handlerCallCount = 0;
            testHandler.CallCountCallback = () => handlerCallCount++;
            
            // Register MediaHub
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(CachingTests).Assembly));
            
            // Registrar el handler como singleton directamente
            services.AddSingleton<IRequestHandler<TestCacheableWithEmptyKeyRequest, string>>(testHandler);
            services.AddTransient<IPipelineBehavior<TestCacheableWithEmptyKeyRequest, string>, CachingBehavior<TestCacheableWithEmptyKeyRequest, string>>();
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            
            var request = new TestCacheableWithEmptyKeyRequest();
            
            // Act
            var firstResponse = await mediator.Send(request);
            var secondResponse = await mediator.Send(request);
            
            // Assert
            firstResponse.Should().Be("Response");
            secondResponse.Should().Be("Response");
            
            // Verify handler was called twice (no caching)
            handlerCallCount.Should().Be(2);
        }

        // Test request & handler classes
        public class TestCacheableRequest : IRequest<string>, ICacheableRequest
        {
            public int Id { get; set; }

            public string CacheKey => $"TestRequest_{Id}";
            public int CacheTime => 10; // cache for 10 minutes
        }
        
        public class TestCacheableRequestHandler : IRequestHandler<TestCacheableRequest, string>
        {
            public Action CallCountCallback { get; set; }
            
            public Task<string> Handle(TestCacheableRequest request, CancellationToken cancellationToken)
            {
                CallCountCallback?.Invoke();
                return Task.FromResult($"Response for {request.Id}");
            }
        }
        
        public class TestCacheableWithEmptyKeyRequest : IRequest<string>, ICacheableRequest
        {
            public string CacheKey => string.Empty; // Empty cache key should bypass caching
            public int CacheTime => 10;
        }
        
        public class TestCacheableWithEmptyKeyRequestHandler : IRequestHandler<TestCacheableWithEmptyKeyRequest, string>
        {
            public Action CallCountCallback { get; set; }
            
            public Task<string> Handle(TestCacheableWithEmptyKeyRequest request, CancellationToken cancellationToken)
            {
                CallCountCallback?.Invoke();
                return Task.FromResult("Response");
            }
        }
    }
}