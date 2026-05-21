# mTLS Bearer Transport for Confidential Client Applications

## What is mTLS Bearer Transport?

By default, MSAL authenticates a confidential client app by signing a JWT (`client_assertion`) with its certificate and including that assertion in the POST body of every token request.

**mTLS bearer transport** is an alternative: the certificate is presented at the **TLS layer** during the handshake, and no `client_assertion` is sent in the request body. The token returned is still a standard Bearer token.

This is enabled by the `SendCertificateOverMtls = true` option. When set:
- Token requests are routed to `mtlsauth.microsoft.com` (or a regional mTLS endpoint when `WithAzureRegion` is also configured)
- `client_assertion` is **not** included in the POST body
- The TLS certificate authenticates the app

## AAD Prerequisite: App Enablement (Preview)

> ⚠️ **This feature is in preview. Your app must be enabled for mTLS client auth by Microsoft Entra before token requests will succeed.**
>
> There is no self-serve portal today. Without enablement, AAD returns `AADSTS51000: MtlsClientAuth is/are disabled`.

## How to Opt In

Two steps are required.

### Step 1 — Configure the credential

```csharp
var cca = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
    .WithCertificate(cert, new CertificateOptions { SendCertificateOverMtls = true })
    .WithHttpClientFactory(new MyMtlsHttpClientFactory(cert))  // see Step 2
    .Build();
```

`SendCertificateOverMtls` requires a certificate-based credential. Passing it with a client secret throws at `Build()` time.

### Step 2 — Implement `IMsalMtlsHttpClientFactory`

MSAL calls `GetHttpClient(X509Certificate2)` to obtain an `HttpClient` that presents the client certificate during the TLS handshake. You must provide an implementation via `WithHttpClientFactory`.

```csharp
public class MyMtlsHttpClientFactory : IMsalMtlsHttpClientFactory
{
    // Reuse HttpClient instances — do NOT create a new one per call (socket exhaustion).
    private readonly HttpClient _mtlsClient;
    private readonly HttpClient _plainClient;

    public MyMtlsHttpClientFactory(X509Certificate2 cert)
    {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        _mtlsClient = new HttpClient(handler);
        _plainClient = new HttpClient();
    }

    // Called for mTLS token requests (cert at TLS layer)
    public HttpClient GetHttpClient(X509Certificate2 cert) => _mtlsClient;

    // Called for non-mTLS requests (e.g., instance discovery)
    public HttpClient GetHttpClient() => _plainClient;
}
```

### Acquire a token

```csharp
// S2S (app-to-app)
AuthenticationResult result = await cca
    .AcquireTokenForClient(scopes)
    .ExecuteAsync();

// On-behalf-of
AuthenticationResult result = await cca
    .AcquireTokenOnBehalfOf(scopes, new UserAssertion(userToken))
    .ExecuteAsync();
```

The same `WithCertificate(cert, new CertificateOptions { SendCertificateOverMtls = true })` configuration applies to all supported flows — no per-call change is needed.

## Supported Flows

| Flow | MSAL API |
|------|----------|
| App-to-app (S2S / client credentials) | `AcquireTokenForClient` |
| On-behalf-of (OBO) | `AcquireTokenOnBehalfOf` |
| Silent / refresh-token redemption | `AcquireTokenSilent`, `AcquireTokenByRefreshToken` |
| Authorization code | `AcquireTokenByAuthorizationCode` |

## How to Verify It's Working

### Option 1 — Check the token endpoint

```csharp
AuthenticationResult result = await cca.AcquireTokenForClient(scopes).ExecuteAsync();

// Should contain "mtlsauth.microsoft.com", not "login.microsoftonline.com"
Console.WriteLine(result.AuthenticationResultMetadata.TokenEndpoint);
```

### Option 2 — Intercept the request (unit/integration tests)

Use a recording `IMsalMtlsHttpClientFactory` (see `RecordingMtlsHttpClientFactory` in `MtlsTransportUserFlowTests.cs`) to capture the outgoing request. Assert:
- URL contains `mtlsauth`
- Body does **not** contain `client_assertion`

## Known Limitations

- **Windows only** — the mTLS client certificate stack depends on `System.Net.Security` behavior that is not supported on Linux in the current test configuration.
- **AAD-side enablement required (preview)** — there is no self-serve portal today; app enablement requires Microsoft Entra configuration.
- **Certificate credential required** — `SendCertificateOverMtls = true` is incompatible with client secrets and throws at `Build()` time.

## Related Docs

- [sni_mtls_bearer_token_design.md](sni_mtls_bearer_token_design.md) — internal design spec
- [mtlspop_architecture.md](mtlspop_architecture.md) — mTLS PoP architecture (distinct from Bearer transport)
