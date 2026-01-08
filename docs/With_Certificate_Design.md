# Draft Spec: `WithCertificate` API – Bearer, mTLS, Dynamic Selection

## 1. Goals

- Keep existing `WithCertificate(X509Certificate2, bool sendX5C)` behavior **unchanged**.
- Support:
  - Standard **Bearer** client‑credential tokens.
  - **mTLS Bearer** (certificate used only for TLS binding).
  - **mTLS PoP** (existing `.WithMtlsProofOfPossession()` flow).
- Add a **delegate‑based** `WithCertificate` that can select a certificate **per request** using `IAppConfig`.

## 2. New types

### 2.1 `ClientCertificateUsage`

```csharp
public enum ClientCertificateUsage
{
    /// Certificate is used to produce a client assertion (private_key_jwt).
    Assertion,

    /// Certificate is used for mutual TLS (client TLS authentication / binding).
    Mtls
}
```

### 2.2 `CertificateConfig`

```csharp
public sealed class CertificateConfig
{
    public X509Certificate2 Certificate { get; init; } = default!;
    public ClientCertificateUsage Usage { get; init; } = ClientCertificateUsage.Assertion;
    public bool SendX5C { get; init; } = false;
}
```

- `Usage = Assertion` → certificate is used for client assertion (current behavior).
- `Usage = Mtls` → certificate is used for client TLS authentication and exposed as `BindingCertificate`.
- `SendX5C` matters only for assertion usage.

## 3. `WithCertificate` overloads

### 3.1 Existing overloads (unchanged)

```csharp
public ConfidentialClientApplicationBuilder WithCertificate(X509Certificate2 certificate);
public ConfidentialClientApplicationBuilder WithCertificate(
    X509Certificate2 certificate,
    bool sendX5C);
```

**Internal mapping:**

```csharp
WithCertificate(new CertificateConfig
{
    Certificate = certificate,
    Usage       = ClientCertificateUsage.Assertion,
    SendX5C     = sendX5COrFalse
});
```

This guarantees full backward compatibility.

### 3.2 New static overload

```csharp
public ConfidentialClientApplicationBuilder WithCertificate(
    CertificateConfig config);
```

- `Usage = Assertion`  
  → sets the “client assertion certificate” and `SendX5C`.
- `Usage = Mtls`  
  → sets the “mTLS certificate” used in TLS and as `AuthenticationResult.BindingCertificate`.

### 3.3 New dynamic overloads (per‑request selection)

```csharp
// Full-featured: caller can choose Assertion vs Mtls dynamically.
public ConfidentialClientApplicationBuilder WithCertificate(
    Func<IAppConfig, CertificateConfig> selector);

// Convenience overload: simplest dynamic form (Assertion by default).
public ConfidentialClientApplicationBuilder WithCertificate(
    Func<IAppConfig, X509Certificate2> selector);
```

**Semantics**

- Selectors are stored in configuration and **evaluated once per `ExecuteAsync()` call**.
- MSAL builds an `IAppConfig` view (ClientId, TenantId / authority, etc.) and passes it to the selector.
- The selected certificate/config:
  - Drives assertion and/or mTLS behavior as per `CertificateConfig.Usage`.
  - Is also surfaced as `appConfig.ClientCredentialCertificate` so retry/observer hooks can log success/failure for the right certificate.

The simple delegate overload behaves as:

```csharp
WithCertificate(ac => new CertificateConfig
{
    Certificate = selector(ac),
    Usage       = ClientCertificateUsage.Assertion
});
```

## 4. Bearer vs mTLS‑PoP (request‑level choice)

Token type remains a **per‑request** decision:

```csharp
// Default: Bearer
await app.AcquireTokenForClient(scopes).ExecuteAsync();

// mTLS PoP
await app.AcquireTokenForClient(scopes)
         .WithMtlsProofOfPossession()
         .ExecuteAsync();
```

Interaction with `ClientCertificateUsage`:

- `Usage = Assertion`, no `.WithMtlsProofOfPossession()`  
  → standard Bearer client‑credential flow (current behavior).

- `Usage = Mtls`, no `.WithMtlsProofOfPossession()`  
  → **mTLS Bearer**:
  - TLS client certificate set from the mTLS cert.
  - `AuthenticationResult.TokenType == "Bearer"`.
  - `AuthenticationResult.BindingCertificate` = mTLS cert.

- `Usage = Mtls` + `.WithMtlsProofOfPossession()`  
  → **mTLS PoP** (current PoP semantics).

## 5. Example usage

### 5.1 Static assertion (legacy)

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(assertionCert)
    .Build();

var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
```

### 5.2 Static mTLS Bearer / mTLS PoP

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(new CertificateConfig
    {
        Certificate = mtlsCert,
        Usage       = ClientCertificateUsage.Mtls,
        SendX5C     = true
    })
    .Build();

// mTLS Bearer
var bearer = await app.AcquireTokenForClient(scopes).ExecuteAsync();

// mTLS PoP
var pop = await app.AcquireTokenForClient(scopes)
                   .WithMtlsProofOfPossession()
                   .ExecuteAsync();
```

### 5.3 Dynamic certificate selection

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(authority)
    .WithCertificate(appConfig =>
        new CertificateConfig
        {
            Certificate = selectionService
                .GetCertificate(appConfig.ClientId, appConfig.TenantId),
            Usage   = ClientCertificateUsage.Mtls,
            SendX5C = true
        })
    .Build();
```
