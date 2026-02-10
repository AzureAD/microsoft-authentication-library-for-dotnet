# Token Caching Strategies

## Overview
MSAL.NET automatically caches tokens. Understanding cache behavior optimizes performance.

## Built-in Token Cache
```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(cert)
    .Build();

// First call - token acquired from AAD
var result1 = await app.AcquireTokenForClient(scopes).ExecuteAsync();

// Subsequent calls - token retrieved from cache
var result2 = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

## Cache Eviction
Tokens are automatically refreshed when:
- Token has expired
- Explicit `ForceRefresh()` is called
- Cache is cleared

## Force Refresh When Needed
```csharp
var result = await app.AcquireTokenForClient(scopes)
    .ForceRefresh(true)
    .ExecuteAsync();
```

## Best Practices
- Use built-in token cache for automatic caching
- Monitor cache hit rates for performance optimization
- Clear cache on credential rotation

## Monitor Cache Performance

Use `AuthenticationResult.AuthenticationResultMetadata` to monitor cache behavior and performance:

```csharp
var result = await app.AcquireTokenForClient(new[] { "resource-uri" }).ExecuteAsync();

Console.WriteLine($"Token Source: {result.TokenSource}");

var metadata = result.AuthenticationResultMetadata;
if (metadata != null)
{
    Console.WriteLine($"Total Duration: {metadata.DurationTotalInMs}ms");
    Console.WriteLine($"Cache Duration: {metadata.DurationInCacheInMs}ms");
    Console.WriteLine($"HTTP Duration: {metadata.DurationInHttpInMs}ms");
    Console.WriteLine($"Refresh Reason: {metadata.CacheRefreshReason}");
}
```

### Key Metrics

| Metric | Description | Expected Values |
|--------|-------------|--------------------|
| **TokenSource** | Where token came from (cache or AAD) | Cache or IdentityProvider |
| **DurationTotalInMs** | Total time in MSAL (cache + HTTP) | ~100ms (cache) vs ~700ms (fresh) |
| **DurationInCacheInMs** | Time spent accessing token cache | Typically <50ms |
| **DurationInHttpInMs** | Time spent in AAD HTTP calls | Typically 300-700ms when fresh |
| **CacheRefreshReason** | Why cache was refreshed (if applicable) | NotFresh, Expired, ForceRefresh, etc. |

### Best Practices
- **Cache Hit**: DurationTotalInMs ~100ms, TokenSource = Cache
- **Fresh Token**: DurationTotalInMs ~700ms, TokenSource = IdentityProvider
- **Alarm on**: DurationTotalInMs > 1 second consistently
- **Monitor**: Cache hit ratio over time to assess performance optimization
