---
title: how to use mTLS proof-of-possession in MSAL(preview)
description: Learn how to acquire mTLS proof-of-possession tokens in MSAL.NET.
ms.service: entra-id
ms.subservice: develop
ms.topic: conceptual
ms.date: 11/17/2025
---

# Use mTLS proof-of-possession (mTLS PoP) tokens in MSAL.NET (internal Microsoft only - preview)

> [!IMPORTANT]
> mTLS proof-of-possession (mTLS PoP) support in MSAL.NET is currently in internal preview.
>
> - For **managed identities**, `WithMtlsProofOfPossession` is enabled by the
>   [`Microsoft.Identity.Client.MtlsPop`](https://www.nuget.org/packages/Microsoft.Identity.Client.MtlsPop) package (preview).
> - For **confidential clients** (SNI and FIC flows), `WithMtlsProofOfPossession` is exposed directly by the MSAL.NET preview package and does **not** require the additional MtlsPoP package.
>
> The resource (API) must be configured to accept mTLS PoP tokens and validate the certificate bound to the token.

## Overview

mTLS PoP builds directly on top of existing MSAL experiences:

- **Managed identity** (system-assigned or user-assigned) via `ManagedIdentityApplicationBuilder` (**requires** the MtlsPoP package).
- **Confidential clients** using **SN/I certificates** via `WithCertificate(...)` (no extra package needed).
- **Federated identity credentials (FIC)** via `WithClientAssertion(...)` (no extra package needed).

Across all these scenarios:

- You still use the same MSAL builders and token acquisition methods.
- You call `.WithMtlsProofOfPossession()` on the token request.
- You use the returned `AuthenticationResult.BindingCertificate` when calling the API over mTLS.

---

## 1. Use managed identities with mTLS proof-of-possession

mTLS PoP builds directly on top of the existing managed identity experience:

- You still use `ManagedIdentityApplicationBuilder`.
- You still call `AcquireTokenForManagedIdentity`.

The only changes are:

- Add the MtlsPoP package (managed identity only).
- Add `.WithMtlsProofOfPossession()` when acquiring the token.
- Use the binding certificate in `AuthenticationResult` when calling the API over mTLS.

### 1.1 Add the MtlsPoP package (managed identity only)

For **managed identity** scenarios, install the latest preview package alongside `Microsoft.Identity.Client`:

```bash
dotnet add package Microsoft.Identity.Client.MtlsPop --version 4.79.1-preview
```

This package:

- exposes the `WithMtlsProofOfPossession()` extension for managed identity flows, and
- brings in a native dependency used to attest managed identity keys (for example KeyGuard keys) via Microsoft Azure Attestation (MAA).

### 1.2 Current experience – Bearer (Graph)

The following example shows how to use either a user-assigned or system-assigned managed identity.

```csharp
using System.Threading.Tasks;
using Microsoft.Identity.Client;

// Choose the appropriate managed identity:
// - For user-assigned MI: ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId)
// - For system-assigned MI: ManagedIdentityId.SystemAssigned

IManagedIdentityApplication mi =
    ManagedIdentityApplicationBuilder
        .Create(
            ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId)
            // or: ManagedIdentityId.SystemAssigned
        )
        .Build();

// Microsoft Graph as the target API
const string graphResource = "https://graph.microsoft.com/";

AuthenticationResult bearerResult = await mi
    .AcquireTokenForManagedIdentity(graphResource)
    .ExecuteAsync()
    .ConfigureAwait(false);

// bearerResult.AccessToken is a Bearer token (bearerResult.TokenType == "Bearer")
```

### 1.3 New experience – mTLS PoP (Graph)

```csharp
using System.Threading.Tasks;
using Microsoft.Identity.Client;

// Choose the appropriate managed identity:
// - For user-assigned MI: ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId)
// - For system-assigned MI: ManagedIdentityId.SystemAssigned

IManagedIdentityApplication mi =
    ManagedIdentityApplicationBuilder
        .Create(
            ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId)
            // or: ManagedIdentityId.SystemAssigned
        )
        .Build();

// Microsoft Graph as the target API
const string graphResource = "https://graph.microsoft.com/";

AuthenticationResult mtlsPopResult = await mi
    .AcquireTokenForManagedIdentity(graphResource)
    .WithMtlsProofOfPossession()   // <-- new API
    .ExecuteAsync()
    .ConfigureAwait(false);

// mtlsPopResult.TokenType == "mtls_pop"
// mtlsPopResult.BindingCertificate is the client cert to use for mTLS
```

---

## 2. Use SNI certificate + confidential client app for mTLS PoP

This section shows how to use mTLS PoP with a **confidential client application** that authenticates using an **SN/I certificate**.

You configure:

- An SN/I certificate on the app via `WithCertificate(...)`,
- A region via `WithAzureRegion("east us")`, and
- `WithMtlsProofOfPossession()` on the token request.

The resulting token is bound to a certificate exposed as `AuthenticationResult.BindingCertificate`, which you then use to call Microsoft Graph over mTLS.

> [!NOTE]
> For SNI-based confidential clients, mTLS PoP is provided by the MSAL.NET preview package itself.
> You do **not** need the `Microsoft.Identity.Client.MtlsPop` package for this scenario.

### 2.1 Current experience – Bearer with SN/I certificate

```csharp
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

X509Certificate2 clientCertificate = LoadSnICertificate();

IConfidentialClientApplication app =
    ConfidentialClientApplicationBuilder
        .Create(clientId)
        .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
        .WithAzureRegion("east us")            // Region example; required for SNI
        .WithCertificate(clientCertificate, sendX5C: true)
        .Build();

string[] scopes = new[] { "https://graph.microsoft.com/.default" };

AuthenticationResult result = await app
    .AcquireTokenForClient(scopes)
    .ExecuteAsync()
    .ConfigureAwait(false);

// result.TokenType will return "Bearer"
```

### 2.2 New experience – mTLS PoP with SN/I certificate

```csharp
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

X509Certificate2 clientCertificate = LoadSnICertificate();

IConfidentialClientApplication app =
    ConfidentialClientApplicationBuilder
        .Create(clientId)
        .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
        .WithAzureRegion("east us")            // Required for SNI
        .WithCertificate(clientCertificate, sendX5C: true)
        .Build();

string[] scopes = new[] { "https://graph.microsoft.com/.default" };

AuthenticationResult result = await app
    .AcquireTokenForClient(scopes)
    .WithMtlsProofOfPossession()             // <-- new mTLS PoP API
    .ExecuteAsync()
    .ConfigureAwait(false);

// result.TokenType will return "mtls_pop"
// result.BindingCertificate is the certificate that the token is bound to.
// For SN/I flows, this is the same certificate passed to WithCertificate.

await GraphCaller.CallGraphWithMtlsPopAsync(result, CancellationToken.None);
```

> `LoadSnICertificate()` is an app-specific helper that loads your SN/I certificate
> (for example, from the CurrentUser/My store or a Key Vault-backed certificate).

---

## 3. Use mTLS PoP with federated identity credentials (FIC)

You can also combine mTLS proof-of-possession with **federated identity credentials (FIC)**.

In this case, the confidential client:

- Uses `WithClientAssertion(...)` instead of a secret or certificate.
- Provides a `ClientSignedAssertion` that includes:
  - the FIC assertion (`Assertion`), and
  - the certificate used for mTLS binding (`TokenBindingCertificate`).
- Acquires tokens with `WithMtlsProofOfPossession()`.

> [!NOTE]
> As with the SNI scenario, FIC-based confidential clients use the `WithMtlsProofOfPossession()` support built into the MSAL.NET preview package and do **not** require the MtlsPoP package.

```csharp
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig; // AssertionRequestOptions, ClientSignedAssertion

// Application-specific helper that creates the FIC assertion (JWT)
private static string CreateFicJwt(AssertionRequestOptions options, X509Certificate2 bindingCertificate)
{
    // TODO: create and sign a JWT according to your FIC configuration
    // (issuer, subject, audience, claims, expiry, etc.)
    return "<your-fic-jwt>";
}

// Delegate used by MSAL to obtain a client assertion + binding certificate
private static Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>>
CreateFicAssertion(X509Certificate2 bindingCertificate)
{
    return (options, cancellationToken) =>
    {
        string jwt = CreateFicJwt(options, bindingCertificate);

        return Task.FromResult(new ClientSignedAssertion
        {
            Assertion = jwt,
            TokenBindingCertificate = bindingCertificate
        });
    };
}

public static async Task AcquireTokenWithFicAndMtlsPopAsync(
    string clientId,
    string tenantId,
    X509Certificate2 bindingCertificate,
    CancellationToken cancellationToken = default)
{
    if (bindingCertificate is null)
    {
        throw new ArgumentNullException(nameof(bindingCertificate));
    }

    // Configure the confidential client to use FIC via client assertion
    IConfidentialClientApplication app =
        ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
            .WithAzureRegion("east us") // required for SNI
            .WithClientAssertion(CreateFicAssertion(bindingCertificate))
            .Build();

    string[] scopes = new[] { "https://graph.microsoft.com/.default" };

    AuthenticationResult result = await app
        .AcquireTokenForClient(scopes)
        .WithMtlsProofOfPossession()          // <-- enable mTLS PoP on this flow
        .ExecuteAsync(cancellationToken)
        .ConfigureAwait(false);

    // result.TokenType == "mtls_pop"
    // result.BindingCertificate matches the certificate used in the assertion
    // (TokenBindingCertificate). You can now call Graph over mTLS using the
    // shared helper in "Call Microsoft Graph with an mTLS PoP token":
    await GraphCaller.CallGraphWithMtlsPopAsync(result, cancellationToken);
}
```

> In both managed identity and SN/I + FIC cases, the `BindingCertificate` and `AccessToken`
> from the `AuthenticationResult` can be reused:
> - as the certificate for the mTLS connection to the resource, and
> - as the access token you send in the `Authorization` header.

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
```
