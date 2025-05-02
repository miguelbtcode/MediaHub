namespace MediaHub.Caching
{
    /// <summary>
    /// Interface for cacheable requests
    /// </summary>
    public interface ICacheableRequest
    {
        /// <summary>
        /// Cache key for the request
        /// </summary>
        string CacheKey { get; }

        /// <summary>
        /// Cache time in minutes
        /// </summary>
        int CacheTime { get; }
    }
}