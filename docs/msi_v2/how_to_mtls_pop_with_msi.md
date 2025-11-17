---
title: Use managed identities with mTLS proof-of-possession (preview)
description: Learn how to use managed identity with mTLS proof-of-possession tokens in MSAL.NET.
ms.service: entra-id
ms.subservice: develop
ms.topic: conceptual
ms.date: 11/17/2025
---

# Use managed identities with mTLS proof-of-possession (internal microsoft only - preview)

> [!IMPORTANT]
> mTLS proof-of-possession (mTLS PoP) for managed identities is currently in internal preview.
>
> To use `WithMtlsProofOfPosession`, you must add the package
> [`Microsoft.Identity.Client.MtlsPop`](https://www.nuget.org/packages/Microsoft.Identity.Client.MtlsPop) (for example, version `4.79.1-preview`).
>
> The resource (API) must be configured to accept mTLS PoP tokens and validate the certificate bound to the token.

mTLS PoP builds directly on top of the existing managed identity experience:

- You still use `ManagedIdentityApplicationBuilder`.
- You still call `AcquireTokenForManagedIdentity`.

The only changes are:

- Build Managed Identity app [using MSAL](https://learn.microsoft.com/en-us/entra/msal/dotnet/advanced/managed-identity). 
- Add the MtlsPoP package.
- Add `.WithMtlsProofOfPosession()` when acquiring the token.
- Use the returned binding certificate when calling the API over mTLS.

Below we show the current (Bearer) code first, then the new (mTLS PoP) version, using Microsoft Graph as the example API.

## 1. Add the MtlsPoP package

Install the preview package alongside `Microsoft.Identity.Client`:

```bash
dotnet add package Microsoft.Identity.Client.MtlsPop --version 4.79.1-preview
```

This package:

- exposes the `WithMtlsProofOfPosession()` extension, and
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

// result.AccessToken is a Bearer token
// result.TokenType == "Bearer"
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
    .WithMtlsProofOfPosession()   // <-- new API
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
    .WithMtlsProofOfPosession()   // <-- new API
    .ExecuteAsync()
    .ConfigureAwait(false);

// result.TokenType == "mtls_pop"
```

---

## 4. Call Microsoft Graph with an mTLS PoP token

Once you have an `AuthenticationResult` from `WithMtlsProofOfPosession()`:

- `result.TokenType` will be `"mtls_pop"`.
- `result.BindingCertificate` is the certificate that the token is bound to.

Use that certificate for the mTLS handshake, and send the token in the `Authorization` header:

```csharp
// Use the certificate returned by MSAL for the mTLS handshake
using var handler = new HttpClientHandler();
handler.ClientCertificates.Add(result.BindingCertificate);

// Create an HttpClient that uses mTLS
using var httpClient = new HttpClient(handler);

// Example: call Microsoft Graph
var request = new HttpRequestMessage(
    HttpMethod.Get,
    "https://graph.microsoft.com/v1.0/me");

// Use the token type and token from MSAL
request.Headers.Authorization =
    new AuthenticationHeaderValue(result.TokenType, result.AccessToken);
// result.TokenType == "mtls_pop"

HttpResponseMessage response = await httpClient.SendAsync(request);
response.EnsureSuccessStatusCode();
```

> In production, you should cache `HttpClient` instances per certificate to avoid socket exhaustion.
