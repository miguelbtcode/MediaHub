using MediaHub.Caching;
using MediaHub.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaHub.Behaviors
{
    /// <summary>
    /// Pipeline behavior to handle caching
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>, ICacheableRequest
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var cacheKey = request.CacheKey;
            
            if (string.IsNullOrEmpty(cacheKey))
            {
                return await next();
            }

            if (_cache.TryGetValue(cacheKey, out TResponse cachedResponse))
            {
                _logger.LogInformation("Returning cached response for {CacheKey}", cacheKey);
                return cachedResponse;
            }

            var response = await next();

            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(request.CacheTime));

            return response;
        }
    }
}