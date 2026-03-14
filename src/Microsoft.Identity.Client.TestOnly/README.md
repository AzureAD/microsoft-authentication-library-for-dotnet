# Microsoft.Identity.Client.TestOnly

Public test framework for [Microsoft.Identity.Client](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) (MSAL.NET).

## Purpose

Provides mock HTTP infrastructure, test helpers, and utilities for testing applications that use MSAL, including:
- Managed identity token acquisition
- mTLS Proof-of-Possession (PoP)
- Attestation support
- Certificate binding
- Multi-tenant scenarios

## Installation

```
dotnet add package Microsoft.Identity.Client.TestOnly
```

> **Note:** This package is intended for use **only in test projects**. Do not reference it in production code.

## Usage

### Basic MSI token mocking

```csharp
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.TestOnly;

using var httpManager = new MockHttpManager();

httpManager.AddMockHandler(
    MockHelpers.CreateMsiTokenHandler(
        accessToken: "mock-token",
        resource: "https://management.azure.com/"));

var app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .WithHttpManager(httpManager)   // extension method from TestOnly
    .Build();

var result = await app
    .AcquireTokenForManagedIdentity("https://management.azure.com/.default")
    .ExecuteAsync();

Assert.Equal("mock-token", result.AccessToken);
```

### mTLS PoP with attestation (system-assigned)

```csharp
using var httpManager = new MockHttpManager();

// Adds all three IMDS v2 mock handlers in one call
httpManager.AddManagedIdentityMtlsTokenMocks();

var app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .WithHttpManager(httpManager)
    .Build();

var result = await app
    .AcquireTokenForManagedIdentity("https://management.azure.com/.default")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

Assert.NotNull(result.BindingCertificate);
Assert.Equal("mtls_pop", result.TokenType);
```

### mTLS PoP with user-assigned managed identity

```csharp
using var httpManager = new MockHttpManager();

httpManager.AddManagedIdentityMtlsTokenMocks(
    userAssignedIdentityId: UserAssignedIdentityId.ClientId,
    userAssignedId: "04ca4d6a-c720-4ba1-aa06-f6634b73fe7a");

var app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.WithUserAssignedClientId("04ca4d6a-c720-4ba1-aa06-f6634b73fe7a"))
    .WithHttpManager(httpManager)
    .Build();

var result = await app
    .AcquireTokenForManagedIdentity("https://management.azure.com/.default")
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

Assert.NotNull(result.BindingCertificate);
```

### Cached certificate refresh

```csharp
using var httpManager = new MockHttpManager();

// First acquisition — mints and caches the binding certificate (3 HTTP calls)
httpManager.AddManagedIdentityMtlsTokenMocks();

var app = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .WithHttpManager(httpManager)
    .Build();

const string scope = "https://management.azure.com/.default";

var result1 = await app
    .AcquireTokenForManagedIdentity(scope)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

// Force-refresh — certificate is cached, so only 2 HTTP calls
httpManager.AddManagedIdentityMtlsTokenMocks_CachedCertRefresh();

var result2 = await app
    .AcquireTokenForManagedIdentity(scope)
    .WithForceRefresh(true)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();

Assert.Equal(result1.BindingCertificate.Thumbprint, result2.BindingCertificate.Thumbprint);
```

### Test logging

```csharp
var logger = new TestIdentityLogger(EventLogLevel.Verbose);

var app = ConfidentialClientApplicationBuilder
    .Create("client-id")
    .WithLogging(logger)
    .Build();

// ... run flow ...

var log = logger.StringBuilder.ToString();
Assert.Contains("expected text", log);
```

## API reference

See XML documentation on each class for detailed method signatures and examples.

| Type | Description |
|------|-------------|
| `MockHttpManager` | Queue-based mock HTTP manager; inject into MSAL builders via `WithHttpManager()`. |
| `MockHttpMessageHandler` | Configurable `HttpClientHandler` with built-in request validation. |
| `MockHttpClientFactory` | `IMsalMtlsHttpClientFactory` backed by a handler queue. |
| `MockMtlsHttpClientFactory` | Single-handler `IMsalMtlsHttpClientFactory` for simple mTLS tests. |
| `MockHelpers` | Factory methods for common mock handlers (MSI, instance discovery, mTLS). |
| `ManagedIdentityMtlsTestHelpers` | High-level extension methods for full mTLS PoP mock setup. |
| `ManagedIdentityTestConstants` | Shared constants (certificates, tenant ID, endpoints). |
| `TestIdentityLogger` | Accumulates log messages from MSAL into a `StringBuilder`. |
| `UserAssignedIdentityId` | Enum identifying how a UAMI is specified in mock query params. |
| `ApplicationBuilderExtensions` | `WithHttpManager()` extension for MSAL application builders. |

## Supported frameworks

- .NET Framework 4.6.2+
- .NET 6.0+
- .NET 8.0+

## License

MIT
