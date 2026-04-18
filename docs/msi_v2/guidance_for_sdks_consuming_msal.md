# Guidance for SDKs Consuming MSAL: HttpClient Factory with mTLS PoP

## Context

MSAL provides the `WithHttpClientFactory()` API so that higher-level SDKs and applications can customize the `HttpClient` used for all HTTP calls. Common reasons include:

- **HTTP proxy configuration** — routing traffic through corporate proxies
- **Custom retry logic** — SDK-specific retry policies and resilience patterns
- **Telemetry and logging** — injecting headers, tracing, and diagnostics
- **Connection pooling** — using ASP.NET Core's `IHttpClientFactory` for scalable apps

This is documented in the [MSAL.NET HttpClient wiki](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/HttpClient) and is a supported, common pattern.

With MSI v2 mTLS PoP, this customization has new constraints. The mTLS flow requires a **binding certificate** in the TLS handshake — but that certificate is **internal to MSAL** and discovered at runtime from IMDS. The SDK does not have it at factory configuration time.

This document explains how the factory interacts with mTLS, what works, what doesn't, and how SDKs should update their integration.

**Bottom line:** You can still customize the HttpClient, but you must implement `IMsalMtlsHttpClientFactory` instead of `IMsalHttpClientFactory` — otherwise the mTLS flow will fail.

---

## Background: How the MSI v2 mTLS Flow Works

MSAL acquires an mTLS Proof-of-Possession (PoP) token in three internal HTTP calls:

| Step | Purpose | mTLS cert needed? |
|------|---------|-------------------|
| 1 | Get CSR metadata from IMDS | No |
| 2 | Submit CSR to IMDS, receive binding certificate | No |
| 3 | Call mTLS token endpoint | **Yes** — cert must be in the TLS handshake |

The binding certificate only becomes available after Step 2. MSAL manages it internally and exposes it to the developer after token acquisition via `AuthenticationResult.BindingCertificate`.

---

## Interfaces

### `IMsalHttpClientFactory`

The standard factory interface:

```csharp
public interface IMsalHttpClientFactory
{
    HttpClient GetHttpClient();
}
```

No awareness of mTLS certificates. When MSAL needs to make an mTLS call and the factory only implements this interface, MSAL calls `GetHttpClient()` — the certificate is **not included** in the handshake.

### `IMsalMtlsHttpClientFactory`

Extends the standard factory with a certificate-aware overload:

```csharp
public interface IMsalMtlsHttpClientFactory : IMsalHttpClientFactory
{
    HttpClient GetHttpClient(X509Certificate2 x509Certificate2);
}
```

When the factory implements this interface, MSAL calls `GetHttpClient(cert)` for mTLS calls — **passing the binding certificate it discovered from IMDS**. The factory is responsible for attaching the certificate to the `HttpClientHandler`.

### How MSAL resolves the factory

```
if (factory is IMsalMtlsHttpClientFactory mtlsFactory)
    → mtlsFactory.GetHttpClient(cert)     // MSAL passes the cert
else
    → factory.GetHttpClient()              // cert is NOT passed
```

---

## Scenarios

All four scenarios were tested end-to-end on an Azure VM with MSI v2 enabled.

### Scenario 1: `IMsalHttpClientFactory` — no certificate

The SDK provides a custom factory implementing only `IMsalHttpClientFactory`.

```csharp
var app = ManagedIdentityApplicationBuilder.Create(miId)
    .WithHttpClientFactory(new MyPlainFactory())  // implements IMsalHttpClientFactory only
    .Build();

var result = await app.AcquireTokenForManagedIdentity(scope)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

**What happens:** MSAL calls `GetHttpClient()` for the mTLS token endpoint call. No certificate in the TLS handshake.

**Result:** ❌ Fails — `AADSTS392200: Client certificate is missing from the request.`

---

### Scenario 2: `IMsalHttpClientFactory` — SDK bakes certificate from prior AuthResult

The SDK performs a two-pass flow: first acquire a token with the default factory to get the binding certificate, then rebuild the application with the certificate baked into the custom factory.

```csharp
// Pass 1: Default factory — get the certificate
var app1 = ManagedIdentityApplicationBuilder.Create(miId).Build();
var result1 = await app1.AcquireTokenForManagedIdentity(scope)
    .WithMtlsProofOfPossession().ExecuteAsync();
var cert = result1.BindingCertificate;

// Pass 2: Rebuild with custom factory, bake certificate in
var app2 = ManagedIdentityApplicationBuilder.Create(miId)
    .WithHttpClientFactory(new MyFactoryWithCert(cert))
    .Build();
var result2 = await app2.AcquireTokenForManagedIdentity(scope)
    .WithMtlsProofOfPossession().WithForceRefresh(true).ExecuteAsync();
```

**What happens:** The factory returns an `HttpClient` with the certificate already attached. MSAL calls `GetHttpClient()` (no cert parameter), but the handler has the certificate from the prior AuthResult.

**Result:** ✅ Works — but requires two-pass flow and application rebuild. Poor developer experience.

---

### Scenario 3: `IMsalMtlsHttpClientFactory` — no certificate from SDK (recommended)

The SDK provides a factory implementing `IMsalMtlsHttpClientFactory`. No certificate is needed upfront — MSAL passes the certificate when it needs one.

```csharp
var app = ManagedIdentityApplicationBuilder.Create(miId)
    .WithHttpClientFactory(new MyMtlsFactory())  // implements IMsalMtlsHttpClientFactory
    .Build();

var result = await app.AcquireTokenForManagedIdentity(scope)
    .WithMtlsProofOfPossession()
    .ExecuteAsync();
```

**Factory implementation:**

```csharp
class MyMtlsFactory : IMsalMtlsHttpClientFactory
{
    public HttpClient GetHttpClient()
    {
        // Non-mTLS calls (IMDS metadata, CSR submission)
        var handler = new HttpClientHandler();
        ConfigureRetryAndTelemetry(handler);  // SDK customizations
        return new HttpClient(handler);
    }

    public HttpClient GetHttpClient(X509Certificate2 cert)
    {
        if (cert == null)
            return GetHttpClient();

        // MSAL passes the binding certificate — attach it
        var handler = new HttpClientHandler();
        ConfigureRetryAndTelemetry(handler);  // same SDK customizations
        handler.ClientCertificates.Add(cert); // cert from MSAL
        return new HttpClient(handler);
    }
}
```

**What happens during the flow:**

| Call | Purpose | Factory method called | Certificate |
|------|---------|----------------------|-------------|
| 1 | CSR metadata from IMDS | `GetHttpClient(null)` | None needed |
| 2 | CSR submission to IMDS | `GetHttpClient()` | None needed |
| 3 | mTLS token endpoint | `GetHttpClient(cert)` | **MSAL passes the binding cert** |

**Result:** ✅ Works — single pass, no rebuild, no two-step flow.

---

### Scenario 4: `IMsalMtlsHttpClientFactory` — SDK also adds own certificate

Same as Scenario 3, but the SDK also has a certificate from a prior `AuthenticationResult` and adds it alongside MSAL's certificate.

**Result:** ✅ Works — but redundant. MSAL's certificate and the SDK's certificate are the same (both come from the same binding in the certificate store).

---

## Summary

| Scenario | Factory Interface | Cert from SDK? | Passes | App Rebuilds | Works? | Dev Experience |
|----------|-------------------|---------------|--------|-------------|--------|----------------|
| 1 | `IMsalHttpClientFactory` | No | 1 | 0 | ❌ | N/A |
| 2 | `IMsalHttpClientFactory` | Yes (baked in) | 2 | 1 | ✅ | Poor |
| 3 | `IMsalMtlsHttpClientFactory` | No | 1 | 0 | ✅ | **Good** |
| 4 | `IMsalMtlsHttpClientFactory` | Yes (also adds) | 2 | 1 | ✅ | Unnecessary |

---

## Migration Guide for Higher-Level SDKs

SDKs that customize MSAL's HttpClient today need to update their factory to support mTLS. The change is additive — implement `IMsalMtlsHttpClientFactory` (which extends `IMsalHttpClientFactory`) and add one method.

### What needs to change

1. **Implement `IMsalMtlsHttpClientFactory`** instead of `IMsalHttpClientFactory`
2. **Add the `GetHttpClient(X509Certificate2)` overload** that attaches the certificate MSAL passes
3. **Preserve all existing customizations** (retry, telemetry, proxy, etc.) in both overloads

### Before (breaks mTLS):

```csharp
class SdkHttpClientFactory : IMsalHttpClientFactory
{
    public HttpClient GetHttpClient()
    {
        var handler = new HttpClientHandler();
        ConfigureRetryAndTelemetry(handler);
        return new HttpClient(handler);
    }
}
```

### After (supports mTLS):

```csharp
class SdkHttpClientFactory : IMsalMtlsHttpClientFactory
{
    public HttpClient GetHttpClient()
    {
        var handler = new HttpClientHandler();
        ConfigureRetryAndTelemetry(handler);
        return new HttpClient(handler);
    }

    public HttpClient GetHttpClient(X509Certificate2 cert)
    {
        if (cert == null)
            return GetHttpClient();

        var handler = new HttpClientHandler();
        ConfigureRetryAndTelemetry(handler);
        handler.ClientCertificates.Add(cert);
        return new HttpClient(handler);
    }
}
```

### Key points

- **No cert management required.** MSAL discovers the binding certificate from IMDS and passes it to the factory. The SDK does not need to acquire, store, or rotate the certificate.
- **The cert parameter can be null.** For non-mTLS calls (IMDS metadata, CSR submission), the cert is null — fall back to the plain overload.
- **Single pass.** No two-step flow or application rebuild needed.
- **Breaking if not updated.** If the SDK continues to implement only `IMsalHttpClientFactory`, mTLS PoP will fail at runtime with `AADSTS392200: Client certificate is missing from the request`.

---

## Known Gap

**Scenario 1 fails silently.** When a custom `IMsalHttpClientFactory` (without the mTLS interface) is used with mTLS PoP, MSAL does not warn at configuration time. The failure only surfaces at runtime when the token endpoint rejects the request.

A potential improvement would be for MSAL to detect this mismatch early — either at build time or at request time — and emit a clear warning or exception guiding the developer to implement `IMsalMtlsHttpClientFactory`.
