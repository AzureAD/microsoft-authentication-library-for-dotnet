# Token Caching Strategies

## Overview
MSAL.NET automatically caches tokens. Understanding cache behavior and configuration options optimizes performance.

For more detailed information on token cache serialization and caching options, see [Token cache serialization in MSAL.NET](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization?tabs=msal).

## Built-in Token Cache

By default, MSAL.NET caches tokens in-memory. The token cache is automatically managed and refreshed:

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(cert)
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}/v2.0")
    .Build();

// First call - token acquired from AAD
var result1 = await app.AcquireTokenForClient(new[] { "resource-uri" }).ExecuteAsync();

// Subsequent calls - token retrieved from cache
var result2 = await app.AcquireTokenForClient(new[] { "resource-uri" }).ExecuteAsync();
```

## Static Token Caching (Shared Cache)

Enable static token caching to share the token cache across multiple MSAL application instances for improved performance:

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithCertificate(cert)
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}/v2.0")
    .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)  // Enable shared token cache
    .Build();

// Token cache is now shared across multiple app instances
var result = await app.AcquireTokenForClient(new[] { "resource-uri" }).ExecuteAsync();
```

**When to use static caching:**
- Service-to-service applications using `AcquireTokenForClient`
- Apps serving **fewer than ~100,000 tenants**
- Single-process scenarios or process-affinity situations
- When maximum performance is needed

**When NOT to use static caching:**
- Web apps or web APIs (use distributed cache instead)
- Multi-tenant services serving many tenants (>100,000)
- Scenarios requiring cache size control
- Apps needing eviction policies

**Important limitation:**
No way to control cache size with this option. Tokens for different users, tenants, and resources accumulate without eviction policies.

## Cache Eviction
Tokens are automatically refreshed when:
- Token has expired
- Explicit `ForceRefresh()` is called
- Cache is cleared

## Force Refresh When Needed
```csharp
var result = await app.AcquireTokenForClient(new[] { "resource-uri" })
    .ForceRefresh(true)
    .ExecuteAsync();
```

## Best Practices
- Use default built-in caching for most scenarios
- Enable static token caching only for service-to-service apps with moderate tenant counts
- Monitor cache performance to verify caching is working
- Clear cache on credential rotation
- For web apps/APIs or high-scale scenarios, use distributed token cache (see documentation link above)

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
