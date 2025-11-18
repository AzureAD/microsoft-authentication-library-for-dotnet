---
title: Use managed identities with mTLS proof-of-possession (preview)
description: Learn how to use managed identity with mTLS proof-of-possession tokens in MSAL.NET.
ms.service: entra-id
ms.subservice: develop
ms.topic: conceptual
ms.date: 11/17/2025
---

# Use managed identities with mTLS proof-of-possession (internal Microsoft only - preview)

> [!IMPORTANT]
> mTLS proof-of-possession (mTLS PoP) for managed identities is currently in internal preview.
>
> To use `WithMtlsProofOfPossession`, you must add the package
> [`Microsoft.Identity.Client.MtlsPop`](https://www.nuget.org/packages/Microsoft.Identity.Client.MtlsPop) (for example, version `4.79.1-preview`).
>
> The resource (API) must be configured to accept mTLS PoP tokens and validate the certificate bound to the token.

mTLS PoP builds directly on top of the existing managed identity experience:

- You still use `ManagedIdentityApplicationBuilder`.
- You still call `AcquireTokenForManagedIdentity`.

The only changes are:

- Build Managed Identity app [using MSAL](https://learn.microsoft.com/en-us/entra/msal/dotnet/advanced/managed-identity). 
- Add the MtlsPoP package.
- Add `.WithMtlsProofOfPossession()` when acquiring the token.
- Use the returned binding certificate when calling the API over mTLS.

Below we show the current (Bearer) code first, then the new (mTLS PoP) version, using Microsoft Graph as the example API.

## 1. Add the MtlsPoP package

Install the preview package alongside `Microsoft.Identity.Client`:

```bash
dotnet add package Microsoft.Identity.Client.MtlsPop --version 4.79.1-preview
```

This package:

- exposes the `WithMtlsProofOfPossession()` extension, and
- brings in a native dependency used to attest managed identity keys (for example KeyGuard keys) via Microsoft Azure Attestation (MAA).

---

## 2. System-assigned managed identity – from Bearer to mTLS PoP (Graph)

### Current experience – Bearer (Graph)

```csharp
// System-assigned managed identity
IManagedIdentityApplication mi =
    ManagedIdentityApplicationBuilder
        .Create(ManagedIdentityId.SystemAssigned)
        .Build();

// Microsoft Graph as the target API
const string graphScope = "https://graph.microsoft.com/";

AuthenticationResult result = await mi
    .AcquireTokenForManagedIdentity(graphScope)
    .ExecuteAsync()
    .ConfigureAwait(false);

// result.AccessToken is a Bearer token (result.TokenType == "Bearer")
```

### New experience – mTLS PoP (Graph)

```csharp
// System-assigned managed identity
IManagedIdentityApplication mi =
    ManagedIdentityApplicationBuilder
        .Create(ManagedIdentityId.SystemAssigned)
        .Build();

// Microsoft Graph as the target API
const string graphScope = "https://graph.microsoft.com/";

AuthenticationResult result = await mi
    .AcquireTokenForManagedIdentity(graphScope)
    .WithMtlsProofOfPossession()   // <-- new API
    .ExecuteAsync()
    .ConfigureAwait(false);

// result.TokenType == "mtls_pop"
// result.BindingCertificate is the client cert to use for mTLS
```

---

## 3. User-assigned managed identity – from Bearer to mTLS PoP (Graph)

### Current experience – Bearer (Graph)

```csharp
// User-assigned managed identity
IManagedIdentityApplication mi =
    ManagedIdentityApplicationBuilder
        .Create(ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId))
        .Build();

// Microsoft Graph as the target API
const string graphScope = "https://graph.microsoft.com/";

AuthenticationResult result = await mi
    .AcquireTokenForManagedIdentity(graphScope)
    .ExecuteAsync()
    .ConfigureAwait(false);

// result.TokenType == "Bearer"
```

### New experience – mTLS PoP (Graph)

```csharp
// User-assigned managed identity
IManagedIdentityApplication mi =
    ManagedIdentityApplicationBuilder
        .Create(ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId))
        .Build();

// Microsoft Graph as the target API
const string graphScope = "https://graph.microsoft.com/";

AuthenticationResult result = await mi
    .AcquireTokenForManagedIdentity(graphScope)
    .WithMtlsProofOfPossession()   // <-- new API
    .ExecuteAsync()
    .ConfigureAwait(false);

// result.TokenType == "mtls_pop"
// result.BindingCertificate is the certificate that the token is bound to.
```

---

## 4. Call Microsoft Graph with an mTLS PoP token

Once you have an `AuthenticationResult` from `WithMtlsProofOfPossession()`:

- `result.TokenType` will be `"mtls_pop"`.
- `result.BindingCertificate` is the certificate that the token is bound to.

In production, you should reuse `HttpClient` instances rather than creating a new one per request. The example below caches `HttpClient` instances **per binding certificate**:

```csharp
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

// Cache HttpClient instances per binding certificate (thumbprint)
public static class MtlsHttpClientFactory
{
    private static readonly ConcurrentDictionary<string, HttpClient> s_httpClients =
        new ConcurrentDictionary<string, HttpClient>(StringComparer.OrdinalIgnoreCase);

    public static HttpClient GetClient(X509Certificate2 bindingCertificate)
    {
        if (bindingCertificate is null)
        {
            throw new ArgumentNullException(nameof(bindingCertificate));
        }

        return s_httpClients.GetOrAdd(bindingCertificate.Thumbprint, _ =>
        {
            var handler = new HttpClientHandler();

            // Attach the binding certificate so the connection uses mTLS
            handler.ClientCertificates.Add(bindingCertificate);

            // HttpClient is intentionally not disposed here; it is reused for this certificate.
            var client = new HttpClient(handler, disposeHandler: true);
            // Optionally configure defaults such as Timeout, BaseAddress, etc.
            return client;
        });
    }
}

public static class GraphCaller
{
    public static async Task CallGraphWithMtlsPopAsync(
        AuthenticationResult result,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        // Get or create an HttpClient that uses the binding certificate for mTLS
        HttpClient httpClient = MtlsHttpClientFactory.GetClient(result.BindingCertificate);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://graph.microsoft.com/v1.0/me");

        // Use the token type and token from MSAL (TokenType will be "mtls_pop")
        request.Headers.Authorization =
            new AuthenticationHeaderValue(result.TokenType, result.AccessToken);

        HttpResponseMessage response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        // Handle the response body as needed
    }
}

