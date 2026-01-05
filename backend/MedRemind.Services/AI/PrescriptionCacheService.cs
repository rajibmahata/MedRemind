using System.Security.Cryptography;
using System.Text;
using MedRemind.Services.AI.Agents;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.AI;

/// <summary>
/// Cache service to avoid reprocessing identical OCR text
/// Uses SHA256 hashing for cache key generation
/// </summary>
public class PrescriptionCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<PrescriptionCacheService> _logger;
    private static readonly TimeSpan DEFAULT_CACHE_DURATION = TimeSpan.FromHours(24);
    
    public PrescriptionCacheService(IMemoryCache cache, ILogger<PrescriptionCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    /// <summary>
    /// Generate cache key from OCR text using SHA256 hash
    /// </summary>
    public string GenerateCacheKey(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            return string.Empty;
        }
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(ocrText));
        return Convert.ToBase64String(hash);
    }
    
    /// <summary>
    /// Try to get cached result for OCR text
    /// </summary>
    public bool TryGet(string ocrText, out PrescriptionProcessingResult? result)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            result = null;
            return false;
        }
        
        var key = GenerateCacheKey(ocrText);
        
        if (_cache.TryGetValue(key, out result))
        {
            _logger.LogInformation($"? Cache HIT for key: {key.Substring(0, Math.Min(10, key.Length))}...");
            _logger.LogInformation($"   Saved processing time and API costs!");
            return true;
        }
        
        _logger.LogDebug($"?? Cache MISS for key: {key.Substring(0, Math.Min(10, key.Length))}...");
        result = null;
        return false;
    }
    
    /// <summary>
    /// Cache a processing result
    /// </summary>
    public void Set(string ocrText, PrescriptionProcessingResult result)
    {
        if (string.IsNullOrWhiteSpace(ocrText) || result == null)
        {
            _logger.LogWarning("Cannot cache null or empty data");
            return;
        }
        
        var key = GenerateCacheKey(ocrText);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DEFAULT_CACHE_DURATION,
            Priority = CacheItemPriority.Normal
        };
        
        _cache.Set(key, result, cacheOptions);
        _logger.LogInformation($"?? Cached result for key: {key.Substring(0, Math.Min(10, key.Length))}... (TTL: {DEFAULT_CACHE_DURATION.TotalHours}h)");
    }
    
    /// <summary>
    /// Remove a specific cache entry
    /// </summary>
    public void Remove(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            return;
        }
        
        var key = GenerateCacheKey(ocrText);
        _cache.Remove(key);
        _logger.LogInformation($"??? Removed cached entry for key: {key.Substring(0, Math.Min(10, key.Length))}...");
    }
    
    /// <summary>
    /// Clear all cached entries (not available in IMemoryCache interface, needs custom implementation)
    /// </summary>
    public void Clear()
    {
        // Note: IMemoryCache doesn't provide a Clear method
        // This would require a custom cache implementation or tracking keys
        _logger.LogInformation("?? Cache clear requested (note: IMemoryCache doesn't support full clear)");
    }
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        // Note: Basic statistics - full implementation would require tracking
        return new CacheStatistics
        {
            CacheDuration = DEFAULT_CACHE_DURATION
        };
    }
}

/// <summary>
/// Cache statistics model
/// </summary>
public class CacheStatistics
{
    public TimeSpan CacheDuration { get; set; }
    public int TotalHits { get; set; }
    public int TotalMisses { get; set; }
    public double HitRate => TotalHits + TotalMisses > 0 
        ? (double)TotalHits / (TotalHits + TotalMisses) 
        : 0;
}
