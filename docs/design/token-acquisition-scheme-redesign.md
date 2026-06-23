# `ITokenAcquisitionScheme` — Authentication Scheme Redesign

> Status: Design-review ready  
> Issue: [#5998](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5998)  
> Related: [IdWeb credential architecture proposal (PR #3887)](https://github.com/AzureAD/microsoft-identity-web/pull/3887)

---

## Problem Statement

### Interface proliferation in MSAL

MSAL has shipped three versions of the authentication operation interface:

| Interface | What it added | Status |
|---|---|---|
| `IAuthenticationOperation` | Base: `FormatResult`, `KeyId`, `GetTokenRequestParams` | Shipped |
| `IAuthenticationOperation2` | Async `FormatResultAsync`, `ValidateCachedTokenAsync` | Shipped |
| *(internal fork)* | Passes mTLS certificate to operation after credential evaluation | Shipped internally (tactical fix for CDT in MISE) |

The internal tactical fix enabled CDT + mTLS PoP in MISE (now released), but the
pattern does not scale:

1. **Every new capability requires a new interface version.** The next piece of context
   (e.g., authority, client ID, flags) requires yet another interface.

2. **Context passing is minimal.** The internal hook only has `MtlsCertificate`.
   Adding more data requires changing the context class.

3. **No way for the scheme to declare what it needs.** IdWeb cannot ask "does this
   operation require a certificate?" — it must infer from protocol strings and
   thread `isTokenBinding` manually.

### IdWeb's credential selection is entangled with token type

IdWeb threads `isTokenBinding` through 6+ layers because it has no way to ask
"what does this token scheme need from me?" It manually:

- Converts `MTLS_POP` to `ExtraParameters["IsTokenBinding"] = true` in
  `DefaultAuthorizationHeaderProvider`
- Reads that parameter in `TokenAcquisition` and conditionally calls
  `.WithMtlsProofOfPossession()`
- Splits the CCA cache key with a `-tokenBinding` suffix
- Switches credential wiring: `WithCertificate` vs `WithClientAssertion` vs
  `WithCertificate(cert, SendCertificateOverMtls)` vs `WithClientSecret`

This coupling is why IdWeb's `CredentialDescription` carries mutable runtime state
(`CachedValue`, `Skip`, `UseBoundCredential`).

### MSAL has the right internal concepts, but they're not public

MSAL already has internally:
- `CredentialTransportProtocol` enum (OAuth vs Mtls)
- `CredentialMaterialResolver` producing token request parameters + resolved certificate
- `MtlsPopParametersInitializer` distinguishing implicit bearer-over-mTLS from explicit mTLS PoP

But IdWeb cannot ask MSAL: **"what credential capabilities does this token acquisition need?"**

### The missing seam

> **MSAL exposes a token-acquisition scheme contract that tells IdWeb what kind of
> credential material is required before IdWeb resolves credentials.**

---

## Design

### Core Interface: `ITokenAcquisitionScheme`

Key design split: **Metadata** (static, available before credential resolution) vs
**TokenRequestDescriptor** (runtime, may depend on the selected certificate).

Instances are **per-acquisition** — a new instance is created per token request.
This avoids shared mutable state.

**Lifecycle guarantee:** MSAL invokes the scheme factory exactly once at the start of
`ExecuteAsync` and stores the resulting instance on the request parameters. The same
instance is used for `ConfigureAsync`, descriptor creation, cache validation, token
type validation, and result formatting.

```csharp
/// <summary>
/// Defines how MSAL acquires, formats, and caches tokens for a specific
/// authentication scheme (Bearer, mTLS PoP, Bearer-over-mTLS, etc.).
///
/// Instances are per-acquisition — a new instance is created per token request.
/// Implementations are not required to be thread-safe.
/// </summary>
public interface ITokenAcquisitionScheme
{
    /// <summary>
    /// Static metadata about this scheme. Available immediately after construction.
    /// IdWeb reads this before credential resolution.
    /// Must not change after construction.
    /// </summary>
    TokenAcquisitionSchemeMetadata Metadata { get; }

    /// <summary>
    /// MSAL provides runtime context (e.g., the resolved mTLS certificate).
    /// Called once per ExecuteAsync, before cache lookup and before any network
    /// request, after mTLS transport certificate preflight has resolved.
    /// This fires even on cache hits, ensuring the scheme can populate result
    /// properties (e.g., BindingCertificate) regardless of cache state.
    /// </summary>
    ValueTask ConfigureAsync(
        TokenAcquisitionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the runtime token request descriptor. Called after ConfigureAsync().
    /// May depend on the certificate provided in ConfigureAsync (e.g., KeyId).
    /// </summary>
    TokenRequestDescriptor CreateTokenRequestDescriptor(TokenAcquisitionContext context);

    /// <summary>
    /// Transforms the AuthenticationResult after token acquisition.
    /// </summary>
    ValueTask FormatResultAsync(
        AuthenticationResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a cached token is still usable.
    /// MUST NOT perform network I/O.
    /// </summary>
    ValueTask<bool> ValidateCachedTokenAsync(
        MsalCacheValidationData data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the response token type from ESTS is acceptable.
    /// Default implementations compare against Metadata.ExpectedTokenType
    /// (case-insensitive).
    /// </summary>
    bool AcceptsTokenType(string tokenType);
}
```

### Scheme Metadata (static, pre-resolution)

```csharp
/// <summary>
/// Static metadata about a token acquisition scheme.
/// Available before credential resolution — IdWeb reads this to select credentials.
/// </summary>
public sealed class TokenAcquisitionSchemeMetadata
{
    public TokenAcquisitionSchemeMetadata(
        string schemeId,
        string authorizationHeaderPrefix,
        string expectedTokenType,
        CredentialRequirements? credentialRequirements = null)
    {
        SchemeId = schemeId ?? throw new ArgumentNullException(nameof(schemeId));
        AuthorizationHeaderPrefix = authorizationHeaderPrefix
            ?? throw new ArgumentNullException(nameof(authorizationHeaderPrefix));
        ExpectedTokenType = expectedTokenType
            ?? throw new ArgumentNullException(nameof(expectedTokenType));
        CredentialRequirements = credentialRequirements ?? CredentialRequirements.Default;
    }

    /// <summary>
    /// Stable scheme identifier for caching and telemetry.
    /// E.g., "bearer", "mtls_pop", "bearer_mtls".
    /// </summary>
    public string SchemeId { get; }

    /// <summary>
    /// Authorization header prefix. E.g., "Bearer", "mtls_pop".
    /// </summary>
    public string AuthorizationHeaderPrefix { get; }

    /// <summary>
    /// The token type this scheme expects from ESTS. E.g., "Bearer", "mtls_pop".
    /// </summary>
    public string ExpectedTokenType { get; }

    /// <summary>
    /// What credential material this scheme requires.
    /// IdWeb uses this to select a compatible credential.
    /// </summary>
    public CredentialRequirements CredentialRequirements { get; }
}
```

### Credential Requirements

Models transport, token type, and credential material mode as three separate axes
because:

> **Bearer token + bound credential ≠ mTLS PoP bound token**

```csharp
public sealed class CredentialRequirements
{
    public CredentialRequirements(
        TokenEndpointTransport transport = TokenEndpointTransport.DefaultTls,
        AccessTokenKind accessTokenKind = AccessTokenKind.Bearer,
        ClientCredentialKinds allowedCredentialKinds =
            ClientCredentialKinds.Secret
            | ClientCredentialKinds.Certificate
            | ClientCredentialKinds.SignedAssertion,
        ClientCredentialMaterialMode credentialMaterialMode =
            ClientCredentialMaterialMode.OAuth)
    {
        Transport = transport;
        AccessTokenKind = accessTokenKind;
        AllowedCredentialKinds = allowedCredentialKinds;
        CredentialMaterialMode = credentialMaterialMode;
    }

    /// <summary>
    /// Token endpoint transport: how the HTTP request to Entra is sent.
    /// Controls TLS channel setup / HttpClient selection.
    /// IMPORTANT: This does NOT determine how credential material is resolved.
    /// See <see cref="CredentialMaterialMode"/>.
    /// </summary>
    public TokenEndpointTransport Transport { get; }

    /// <summary>
    /// What kind of access token is being requested.
    /// </summary>
    public AccessTokenKind AccessTokenKind { get; }

    /// <summary>
    /// Which credential kinds can satisfy this scheme.
    /// </summary>
    public ClientCredentialKinds AllowedCredentialKinds { get; }

    /// <summary>
    /// How MSAL resolves credential material for the token request.
    /// - OAuth: normal client-authentication material (secret, JWT bearer assertion)
    /// - MtlsBinding: mTLS-bound credential material (cert at TLS layer + PoP token type)
    ///
    /// Bearer-over-mTLS uses Mtls transport but OAuth credential material mode.
    /// mTLS PoP uses Mtls transport and MtlsBinding credential material mode.
    /// </summary>
    public ClientCredentialMaterialMode CredentialMaterialMode { get; }

    /// <summary>
    /// Default: Bearer over default TLS transport, OAuth material mode, any credential kind.
    /// </summary>
    public static CredentialRequirements Default { get; } = new();
}

/// <summary>
/// Token endpoint transport requirement.
/// </summary>
public enum TokenEndpointTransport
{
    /// <summary>Standard TLS: normal token endpoint connection.</summary>
    DefaultTls = 0,
    /// <summary>mTLS: token endpoint requires client certificate at TLS layer.</summary>
    Mtls = 1,
}

/// <summary>
/// What kind of access token the scheme requests.
/// </summary>
public enum AccessTokenKind
{
    Bearer = 0,
    MtlsPop = 1,
    Pop = 2,
    SshCert = 3,
    Extension = 4,
}

/// <summary>
/// Which client credential kinds can satisfy the scheme.
/// </summary>
[Flags]
public enum ClientCredentialKinds
{
    None = 0,
    Secret = 1,
    Certificate = 2,
    SignedAssertion = 4,
    SignedAssertionWithBindingCertificate = 8,
}

/// <summary>
/// How MSAL resolves credential material for the token request body.
/// This is separate from token endpoint transport.
/// </summary>
public enum ClientCredentialMaterialMode
{
    /// <summary>
    /// Normal OAuth client credential material: client secret in body,
    /// or JWT bearer assertion (client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer).
    /// Used by Bearer and Bearer-over-mTLS schemes.
    /// </summary>
    OAuth = 0,

    /// <summary>
    /// mTLS binding credential material: certificate authenticates at TLS layer,
    /// access token is key-bound (mtls_pop). MSAL uses CredentialTransportProtocol.Mtls
    /// internally.
    /// Used by mTLS PoP scheme.
    /// </summary>
    MtlsBinding = 1,
}
```

### Built-in Scheme Requirements

| Scheme | Transport | AccessTokenKind | AllowedCredentialKinds | CredentialMaterialMode |
|--------|-----------|-----------------|----------------------|----------------------|
| **Bearer** | DefaultTls | Bearer | Secret \| Certificate \| SignedAssertion | OAuth |
| **Bearer over mTLS** | Mtls | Bearer | Certificate \| SignedAssertionWithBindingCertificate | OAuth |
| **mTLS PoP** | Mtls | MtlsPop | Certificate \| SignedAssertionWithBindingCertificate | MtlsBinding |

---

### Transport vs Credential Material Mode

These are two distinct concepts:

| Concept | What it means | Where it matters |
|---------|---------------|-----------------|
| **Token endpoint transport** (`Transport`) | How the HTTP request reaches Entra | TLS channel setup, `HttpClient` selection |
| **Credential material mode** (`CredentialMaterialMode`) | How MSAL asks the client credential to produce token request material | `CredentialMaterialResolver`, POST body parameters, access token type |

**Bearer-over-mTLS** uses mTLS transport to Entra but does not request an `mtls_pop`
access token. MSAL resolves token request material through the OAuth credential
material path. A signed assertion provider may still return a certificate-bound
client assertion for credential presentation to Entra, but the downstream access
token remains a bearer token and `AuthenticationResult.BindingCertificate` remains null.

**mTLS PoP** uses mTLS transport AND mTLS binding credential material mode. It
requests an `mtls_pop` access token, sets `CacheBindingKeyId`, and surfaces
`AuthenticationResult.BindingCertificate`.

**Internal mapping rule** (inside MSAL, not exposed):

```csharp
CredentialTransportProtocol internalMode =
    scheme.Metadata.CredentialRequirements.CredentialMaterialMode
        == ClientCredentialMaterialMode.MtlsBinding
            ? CredentialTransportProtocol.Mtls
            : CredentialTransportProtocol.OAuth;
```

This preserves the existing `SendCertificateOverMtls` bearer behavior while adding
a scheme-driven model. The `CredentialMaterialMode` property is the correct signal
for MSAL's internal credential mode — NOT `Transport`.

---

### mTLS Transport Certificate Preflight

When `CredentialRequirements.Transport == Mtls`, MSAL must resolve the certificate
used for token-endpoint mTLS transport **before** cache lookup and **before**
`scheme.ConfigureAsync`.

This preflight is independent of credential material mode.

- **Certificate credentials:** The transport certificate is the configured certificate.
- **Bearer-over-mTLS signed assertion credentials:** MSAL asks the signed assertion
  provider for its `TokenBindingCertificate` to configure the mTLS HTTP client, while
  still resolving token request material in OAuth mode.
- **mTLS PoP:** The same certificate participates in transport, token binding, and
  result formatting.

After preflight, MSAL calls `ConfigureAsync` with `TokenAcquisitionContext.MtlsCertificate`.

**Signed assertion provider note:** For signed assertion credentials, preflight may
call the provider to discover `TokenBindingCertificate` before cache lookup. On cache
hits, this may be the only provider call. On network requests, MSAL may call the
provider again to produce token request material unless the request stores and reuses
the preflight result. Providers are expected to cache expensive assertion/certificate
work, and MSAL should avoid duplicate invocation where practical.

---

### Input: `TokenAcquisitionContext`

```csharp
public sealed class TokenAcquisitionContext
{
    public X509Certificate2? MtlsCertificate { get; init; }
    public string? ClientId { get; init; }
    public string? Authority { get; init; }
    public TokenRequestFlags RequestedFlags { get; init; }
}

[Flags]
public enum TokenRequestFlags
{
    None = 0,
    MtlsProofOfPossession = 1,
    SignedHttpRequest = 2,
}
```

### Output: `TokenRequestDescriptor` (runtime, post-Configure)

Immutable after construction. Callers build the full dictionary before assigning.

```csharp
public sealed class TokenRequestDescriptor
{
    public TokenRequestDescriptor(
        string schemeId,
        string authorizationHeaderPrefix,
        string expectedTokenType,
        string? cacheBindingKeyId = null,
        IReadOnlyDictionary<string, string>? tokenRequestParameters = null,
        IReadOnlyList<string>? additionalCacheParameters = null,
        TelemetryTokenType telemetryTokenType = TelemetryTokenType.Bearer)
    {
        SchemeId = schemeId ?? throw new ArgumentNullException(nameof(schemeId));
        AuthorizationHeaderPrefix = authorizationHeaderPrefix
            ?? throw new ArgumentNullException(nameof(authorizationHeaderPrefix));
        ExpectedTokenType = expectedTokenType
            ?? throw new ArgumentNullException(nameof(expectedTokenType));
        CacheBindingKeyId = cacheBindingKeyId;
        TokenRequestParameters = tokenRequestParameters is null
            ? ImmutableDictionary<string, string>.Empty
            : tokenRequestParameters as ImmutableDictionary<string, string>
              ?? tokenRequestParameters.ToImmutableDictionary();
        AdditionalCacheParameters = additionalCacheParameters is null
            ? Array.Empty<string>()
            : additionalCacheParameters.ToArray();
        TelemetryTokenType = telemetryTokenType;
    }

    public string SchemeId { get; }
    public string AuthorizationHeaderPrefix { get; }
    public string ExpectedTokenType { get; }

    /// <summary>
    /// Key identifier that partitions token cache entries for schemes whose
    /// cached tokens are associated with a key or binding identity.
    /// Null for ordinary bearer tokens and bearer-over-mTLS.
    ///
    /// For mTLS PoP, this is the x5t#S256 of the binding cert.
    /// For legacy IAuthenticationOperation, this maps from KeyId.
    /// </summary>
    public string? CacheBindingKeyId { get; }

    public IReadOnlyDictionary<string, string> TokenRequestParameters { get; }
    public IReadOnlyList<string> AdditionalCacheParameters { get; }
    public TelemetryTokenType TelemetryTokenType { get; }
}

public enum TelemetryTokenType
{
    Unspecified = 0, Bearer = 1, Pop = 2, SshCert = 3,
    External = 4, Extension = 5, MtlsPop = 6,
}
```

---

## Scheme Definitions (IdWeb-facing)

IdWeb consumes `TokenAcquisitionSchemeDefinition` for pre-resolution metadata access
and deferred scheme creation. This is the only shape IdWeb should use.

```csharp
/// <summary>
/// Pairs static metadata (readable before credential resolution) with a factory
/// (invoked per-acquisition after credential resolution).
/// IdWeb reads Definition.Metadata to select credentials, then passes
/// Definition.Create to MSAL's WithAuthenticationScheme.
/// </summary>
public sealed class TokenAcquisitionSchemeDefinition
{
    public TokenAcquisitionSchemeDefinition(
        TokenAcquisitionSchemeMetadata metadata,
        Func<ITokenAcquisitionScheme> create)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Create = create ?? throw new ArgumentNullException(nameof(create));
    }

    public TokenAcquisitionSchemeMetadata Metadata { get; }
    public Func<ITokenAcquisitionScheme> Create { get; }
}

/// <summary>
/// Pre-built definitions for the built-in schemes.
/// IdWeb uses these; MSAL uses the low-level factories in MsalTokenAcquisitionSchemes.
/// </summary>
public static class MsalTokenAcquisitionSchemeDefinitions
{
    private static readonly TokenAcquisitionSchemeMetadata s_bearerMetadata = new(
        "bearer", "Bearer", "bearer");

    private static readonly TokenAcquisitionSchemeMetadata s_bearerMtlsMetadata = new(
        "bearer_mtls", "Bearer", "bearer",
        new CredentialRequirements(
            transport: TokenEndpointTransport.Mtls,
            accessTokenKind: AccessTokenKind.Bearer,
            allowedCredentialKinds: ClientCredentialKinds.Certificate
                | ClientCredentialKinds.SignedAssertionWithBindingCertificate,
            credentialMaterialMode: ClientCredentialMaterialMode.OAuth));

    private static readonly TokenAcquisitionSchemeMetadata s_mtlsPopMetadata = new(
        "mtls_pop", "mtls_pop", "mtls_pop",
        new CredentialRequirements(
            transport: TokenEndpointTransport.Mtls,
            accessTokenKind: AccessTokenKind.MtlsPop,
            allowedCredentialKinds: ClientCredentialKinds.Certificate
                | ClientCredentialKinds.SignedAssertionWithBindingCertificate,
            credentialMaterialMode: ClientCredentialMaterialMode.MtlsBinding));

    public static TokenAcquisitionSchemeDefinition Bearer { get; } =
        new(s_bearerMetadata, MsalTokenAcquisitionSchemes.CreateBearer);

    public static TokenAcquisitionSchemeDefinition BearerOverMtls { get; } =
        new(s_bearerMtlsMetadata, MsalTokenAcquisitionSchemes.CreateBearerOverMtls);

    public static TokenAcquisitionSchemeDefinition MtlsPop { get; } =
        new(s_mtlsPopMetadata, MsalTokenAcquisitionSchemes.CreateMtlsPop);

    /// <summary>
    /// Maps a protocol scheme string to the appropriate definition.
    /// Fails closed for unknown strings — only null/empty defaults to Bearer.
    /// </summary>
    public static TokenAcquisitionSchemeDefinition FromProtocol(string? protocolScheme)
    {
        if (string.IsNullOrWhiteSpace(protocolScheme) ||
            string.Equals(protocolScheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return Bearer;
        }

        if (string.Equals(protocolScheme, "MTLS_POP", StringComparison.OrdinalIgnoreCase))
        {
            return MtlsPop;
        }

        throw new ArgumentOutOfRangeException(
            nameof(protocolScheme),
            protocolScheme,
            "Unknown token acquisition protocol scheme.");
    }
}
```

## Scheme Factory (low-level)

```csharp
/// <summary>
/// Low-level factory methods. MSAL uses these internally.
/// IdWeb should use MsalTokenAcquisitionSchemeDefinitions instead.
/// </summary>
public static class MsalTokenAcquisitionSchemes
{
    public static ITokenAcquisitionScheme CreateBearer() => new BearerScheme();
    public static ITokenAcquisitionScheme CreateBearerOverMtls() => new BearerOverMtlsScheme();
    public static ITokenAcquisitionScheme CreateMtlsPop() => new MtlsPopScheme();
}
```

---

## MSAL Builder Integration

### Primary API: factory-based `WithAuthenticationScheme`

The primary public API accepts a factory delegate to ensure per-acquisition instances:

```csharp
/// <summary>
/// Sets the authentication scheme for this token acquisition.
/// The factory is invoked exactly once per ExecuteAsync to create a fresh
/// scheme instance. That instance is reused for the entire request lifecycle.
/// </summary>
public AcquireTokenForClientParameterBuilder WithAuthenticationScheme(
    Func<ITokenAcquisitionScheme> schemeFactory);
```

Existing APIs become wrappers using method group syntax:

```csharp
public AcquireTokenForClientParameterBuilder WithMtlsProofOfPossession()
{
    return WithAuthenticationScheme(MsalTokenAcquisitionSchemes.CreateMtlsPop);
}
```

An advanced overload accepting an instance is available for callers that manage
instance lifecycle themselves:

```csharp
/// <summary>
/// Advanced: sets a pre-created scheme instance. Caller is responsible for
/// ensuring the instance is not shared across concurrent token acquisitions.
/// </summary>
public AcquireTokenForClientParameterBuilder WithAuthenticationScheme(
    ITokenAcquisitionScheme scheme);
```

### MSAL Validation at Execution Time

MSAL validates scheme requirements at execution time (fail-fast):

```csharp
// In ConfidentialClientExecutor, after preflight, before ConfigureAsync:
if (scheme.Metadata.CredentialRequirements.Transport == TokenEndpointTransport.Mtls
    && context.MtlsCertificate is null)
{
    throw new MsalClientException(
        MsalError.MtlsCertificateNotProvided,
        MsalErrorMessage.MtlsCertificateNotProvidedMessage);
}
```

Host-side selection (IdWeb using `CredentialRequirements`) is for correct credential
wiring. MSAL-side validation is a safety net.

### Managed Identity

The same `ITokenAcquisitionScheme` abstraction applies to managed identity.
MI can still have a separate builder path; the important part is that `MtlsPopScheme`
and cache/token-type/result-format semantics are shared.

---

## Built-in Schemes

### BearerScheme

```csharp
public sealed class BearerScheme : ITokenAcquisitionScheme
{
    public TokenAcquisitionSchemeMetadata Metadata =>
        MsalTokenAcquisitionSchemeDefinitions.Bearer.Metadata;

    public ValueTask ConfigureAsync(TokenAcquisitionContext context, CancellationToken ct)
        => ValueTask.CompletedTask;

    public TokenRequestDescriptor CreateTokenRequestDescriptor(TokenAcquisitionContext context)
        => new("bearer", "Bearer", "bearer");

    public bool AcceptsTokenType(string tokenType)
        => string.Equals(tokenType, "bearer", StringComparison.OrdinalIgnoreCase);

    public ValueTask FormatResultAsync(AuthenticationResult result, CancellationToken ct)
        => ValueTask.CompletedTask;

    public ValueTask<bool> ValidateCachedTokenAsync(MsalCacheValidationData data, CancellationToken ct)
        => ValueTask.FromResult(true);
}
```

### BearerOverMtlsScheme

```csharp
public sealed class BearerOverMtlsScheme : ITokenAcquisitionScheme
{
    // No _cert field — this scheme does not use the certificate for cache binding
    // or result formatting. Stateless reinforces that bearer-over-mTLS is not a
    // downstream-bound token scheme.

    public TokenAcquisitionSchemeMetadata Metadata =>
        MsalTokenAcquisitionSchemeDefinitions.BearerOverMtls.Metadata;

    public ValueTask ConfigureAsync(TokenAcquisitionContext context, CancellationToken ct)
    {
        if (context.MtlsCertificate is null)
        {
            throw new MsalClientException(
                MsalError.MtlsCertificateNotProvided,
                "BearerOverMtlsScheme requires an mTLS certificate for transport.");
        }

        return ValueTask.CompletedTask;
    }

    public TokenRequestDescriptor CreateTokenRequestDescriptor(TokenAcquisitionContext context)
        => new(
            schemeId: "bearer_mtls",
            authorizationHeaderPrefix: "Bearer",
            expectedTokenType: "bearer");
            // CacheBindingKeyId = null — access token is NOT key-bound.
            // Cert is for client auth to Entra, not downstream token binding.

    public bool AcceptsTokenType(string tokenType)
        => string.Equals(tokenType, "bearer", StringComparison.OrdinalIgnoreCase);

    public ValueTask FormatResultAsync(AuthenticationResult result, CancellationToken ct)
    {
        // Do NOT set result.BindingCertificate.
        // The cert was used for client auth to Entra, not for downstream token binding.
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ValidateCachedTokenAsync(MsalCacheValidationData data, CancellationToken ct)
        => ValueTask.FromResult(true);
}
```

### MtlsPopScheme

```csharp
public sealed class MtlsPopScheme : ITokenAcquisitionScheme
{
    private X509Certificate2? _cert;

    public TokenAcquisitionSchemeMetadata Metadata =>
        MsalTokenAcquisitionSchemeDefinitions.MtlsPop.Metadata;

    public ValueTask ConfigureAsync(TokenAcquisitionContext context, CancellationToken ct)
    {
        _cert = context.MtlsCertificate
            ?? throw new MsalClientException(
                MsalError.MtlsCertificateNotProvided,
                "MtlsPopScheme requires an mTLS certificate for binding.");
        return ValueTask.CompletedTask;
    }

    public TokenRequestDescriptor CreateTokenRequestDescriptor(TokenAcquisitionContext context)
        => new(
            schemeId: "mtls_pop",
            authorizationHeaderPrefix: "mtls_pop",
            expectedTokenType: "mtls_pop",
            cacheBindingKeyId: CoreHelpers.ComputeX5tS256KeyId(_cert!),
            tokenRequestParameters: new Dictionary<string, string>
            {
                ["token_type"] = "mtls_pop"
            },
            telemetryTokenType: TelemetryTokenType.MtlsPop);

    public bool AcceptsTokenType(string tokenType)
        => string.Equals(tokenType, "mtls_pop", StringComparison.OrdinalIgnoreCase);

    public ValueTask FormatResultAsync(AuthenticationResult result, CancellationToken ct)
    {
        // For mTLS PoP, the cert IS the binding certificate for the downstream token.
        result.BindingCertificate = _cert;
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ValidateCachedTokenAsync(MsalCacheValidationData data, CancellationToken ct)
        => ValueTask.FromResult(true);
}
```

---

## Bearer-over-mTLS Scheme Selection

`BearerOverMtlsScheme` is **not** selected from the downstream `ProtocolScheme` alone.
It is selected when the requested access token kind is Bearer but the resolved
credential requires mTLS presentation (e.g., `UseBoundCredential = true`).

The flow is:

```csharp
// 1. Get scheme definition from protocol
TokenAcquisitionSchemeDefinition requestedDefinition =
    MsalTokenAcquisitionSchemeDefinitions.FromProtocol(downstreamApiOptions.ProtocolScheme);

// 2. Resolve credential using definition's metadata
var credential = await credentialResolver.ResolveAsync(
    mergedOptions.ClientCredentials,
    requestedDefinition.Metadata.CredentialRequirements,
    cancellationToken);

// 3. Upgrade to BearerOverMtls if credential requires bound transport
TokenAcquisitionSchemeDefinition effectiveDefinition =
    requestedDefinition.Metadata.CredentialRequirements.AccessTokenKind == AccessTokenKind.Bearer
    && credential.PresentationKind == CredentialPresentationKind.Bound
        ? MsalTokenAcquisitionSchemeDefinitions.BearerOverMtls
        : requestedDefinition;
```

### Credential selection policy

For Bearer requests, credential resolution starts with Bearer-compatible requirements.
A credential whose configuration requires bound presentation returns
`PresentationKind.Bound`. IdWeb then upgrades the effective scheme to BearerOverMtls.

**This does not globally prefer bound credentials over earlier unbound credentials**
unless IdWeb explicitly defines such a preference. Credential ordering remains
the primary selection mechanism — the bound upgrade only fires if the *selected*
credential (by ordering) happens to require bound presentation.

---

## Cache Partitioning

### IdWeb CCA key

```
{ClientId}_{Authority}_{AzureRegion}_{SchemeId}_{CredentialRuntimeKey}
```

- `SchemeId`: from `effectiveDefinition.Metadata.SchemeId`
- `CredentialRuntimeKey`: from `credential.CacheKey` (identifies the resolved credential)

This replaces the manual `-tokenBinding` suffix.

### MSAL token cache key

```
{standard MSAL key}_{SchemeId}_{CacheBindingKeyId}
```

- `CacheBindingKeyId` participates in token cache partitioning **only when the access
  token is key-bound** (mTLS PoP).
- For bearer-over-mTLS, `CacheBindingKeyId` is null — the access token is a standard
  bearer token and MUST NOT be over-partitioned by cert thumbprint.
- The credential identity (`CredentialRuntimeKey`) participates in CCA-level caching
  but NOT token-level cache partitioning.

### Examples

| Scenario | CCA key | Token cache key |
|----------|---------|-----------------|
| Bearer + secret | `{cid}_{auth}_{reg}_bearer_{secretHash}` | `{std}_bearer` |
| Bearer over mTLS + cert | `{cid}_{auth}_{reg}_bearer_mtls_{certThumb}` | `{std}_bearer_mtls` |
| mTLS PoP + cert | `{cid}_{auth}_{reg}_mtls_pop_{certThumb}` | `{std}_mtls_pop_{x5tS256}` |

---

## How IdWeb Uses This

### Before (today)

```csharp
bool isTokenBinding = ExtraParameters["IsTokenBinding"] is true;
var app = GetOrBuildCCA(mergedOptions, isTokenBinding);
var builder = app.AcquireTokenForClient(scopes);
if (isTokenBinding) builder.WithMtlsProofOfPossession();
await WithClientCredentialsAsync(mergedOptions, provider, params, isTokenBinding);
```

### After

```csharp
// 1. Get scheme definition from protocol (read metadata before credential resolution)
TokenAcquisitionSchemeDefinition requestedDefinition =
    MsalTokenAcquisitionSchemeDefinitions.FromProtocol(downstreamApiOptions.ProtocolScheme);

CredentialRequirements requirements =
    requestedDefinition.Metadata.CredentialRequirements;

// 2. Resolve credential using scheme requirements
CredentialResolutionResult credential =
    await credentialResolver.ResolveAsync(
        mergedOptions.ClientCredentials, requirements, cancellationToken);

// 3. Upgrade scheme if credential requires bound transport
TokenAcquisitionSchemeDefinition effectiveDefinition =
    requirements.AccessTokenKind == AccessTokenKind.Bearer
    && credential.PresentationKind == CredentialPresentationKind.Bound
        ? MsalTokenAcquisitionSchemeDefinitions.BearerOverMtls
        : requestedDefinition;

// 4. Build/get CCA with resolved credential wired in
var app = await GetOrBuildConfidentialClientApplicationAsync(
    mergedOptions,
    effectiveDefinition.Metadata.SchemeId,
    credential.CacheKey,
    builder => builder.WithResolvedCredential(credential));
    // Phase two: builder.WithClientCredential(credential.Material)

// 5. Acquire token with scheme factory from definition
var result = await app.AcquireTokenForClient(scopes)
    .WithAuthenticationScheme(effectiveDefinition.Create)
    .ExecuteAsync(cancellationToken);
```

IdWeb no longer decides whether mTLS PoP means `.WithCertificate`,
`.WithClientAssertion`, `.WithMtlsProofOfPossession`, `-tokenBinding`,
or `ProtocolNames.MtlsPop`. The scheme owns token semantics; IdWeb owns
credential resolution and CCA lifecycle.

---

## Phase Two (Optional): Unified Client Credential Input

Not required for the first release, but removes IdWeb's builder switch entirely.

```csharp
public abstract record ClientCredentialMaterial;

public sealed record SecretCredentialMaterial(string Secret)
    : ClientCredentialMaterial;

public sealed record CertificateCredentialMaterial(
    X509Certificate2 Certificate, CertificateOptions? Options = null)
    : ClientCredentialMaterial;

public sealed record SignedAssertionCredentialMaterial(
    Func<AssertionRequestOptions, CancellationToken, Task<ClientSignedAssertion>> Provider)
    : ClientCredentialMaterial;

// On builder:
public ConfidentialClientApplicationBuilder WithClientCredential(
    ClientCredentialMaterial material);
```

---

### Bearer-over-mTLS certificate credential compatibility

For certificate credentials, `BearerOverMtlsScheme` must preserve the effective
behavior of today's `CertificateOptions.SendCertificateOverMtls = true` path:
MSAL must use the certificate for token-endpoint mTLS transport and must generate
the correct OAuth client-authentication material for the token request.

During the transition, IdWeb's `WithResolvedCredential(credential)` may still set
`CertificateOptions.SendCertificateOverMtls = true` when the effective scheme is
`BearerOverMtls`. Long term, `TokenEndpointTransport.Mtls` should be the MSAL-owned
signal and IdWeb should not need to know that builder option.

---

## Bug Fix to Address

In the app-token retry path (`TokenAcquisition.cs`), when a client cert/signed
assertion error occurs, the code computes:

```csharp
string applicationKey = GetApplicationKey(mergedOptions, isTokenBinding: false);
```

even though the current flow may be token binding. This may clear the bearer CCA
slot instead of the token-binding CCA slot.

**Tactical fix** (before full redesign lands):

```csharp
string applicationKey = GetApplicationKey(mergedOptions, isTokenBinding);
```

**Long-term fix** (with scheme redesign):

```csharp
string applicationKey = GetApplicationKey(
    mergedOptions,
    effectiveDefinition.Metadata.SchemeId,
    credential.CacheKey);
```

---

## Backward Compatibility: `LegacySchemeAdapter`

```csharp
internal sealed class LegacySchemeAdapter : ITokenAcquisitionScheme
{
    private readonly IAuthenticationOperation _legacy;

    public TokenAcquisitionSchemeMetadata Metadata { get; }

    public LegacySchemeAdapter(IAuthenticationOperation legacy)
    {
        _legacy = legacy;
        Metadata = new(
            schemeId: legacy.AccessTokenType,
            authorizationHeaderPrefix: legacy.AuthorizationHeaderPrefix,
            expectedTokenType: legacy.AccessTokenType);
            // Default credential requirements — legacy operations do not
            // declare requirements. Hosts requiring requirement-driven
            // selection must use new scheme types.
    }

    public ValueTask ConfigureAsync(
        TokenAcquisitionContext context,
        CancellationToken cancellationToken = default)
    {
        // For the OSS public IAuthenticationOperation/2, ConfigureAsync is a no-op.
        // Internal forks that support credential evaluation hooks would extend
        // this adapter to call the appropriate async hook here.
        return ValueTask.CompletedTask;
    }

    public TokenRequestDescriptor CreateTokenRequestDescriptor(TokenAcquisitionContext context)
    {
        var tokenRequestParameters = new Dictionary<string, string>();
        foreach (var kv in _legacy.GetTokenRequestParams())
        {
            tokenRequestParameters[kv.Key] = kv.Value;
        }

        return new(
            schemeId: _legacy.AccessTokenType,
            authorizationHeaderPrefix: _legacy.AuthorizationHeaderPrefix,
            expectedTokenType: _legacy.AccessTokenType,
            cacheBindingKeyId: _legacy.KeyId,
            tokenRequestParameters: tokenRequestParameters,
            telemetryTokenType: MapTelemetryType(_legacy.TelemetryTokenType));
    }

    public bool AcceptsTokenType(string tokenType)
        => string.Equals(
            tokenType,
            _legacy.AccessTokenType,
            StringComparison.OrdinalIgnoreCase);

    public async ValueTask FormatResultAsync(
        AuthenticationResult result,
        CancellationToken cancellationToken = default)
    {
        if (_legacy is IAuthenticationOperation2 async2)
            await async2.FormatResultAsync(result, cancellationToken).ConfigureAwait(false);
        else
            _legacy.FormatResult(result);
    }

    public ValueTask<bool> ValidateCachedTokenAsync(
        MsalCacheValidationData data,
        CancellationToken cancellationToken = default)
    {
        if (_legacy is IAuthenticationOperation2 async2)
            return new ValueTask<bool>(async2.ValidateCachedTokenAsync(data));
        return ValueTask.FromResult(true);
    }

    private static TelemetryTokenType MapTelemetryType(int legacyType)
        => legacyType switch
        {
            1 => TelemetryTokenType.Bearer,
            2 => TelemetryTokenType.Pop,
            3 => TelemetryTokenType.SshCert,
            4 => TelemetryTokenType.External,
            5 => TelemetryTokenType.Extension,
            6 => TelemetryTokenType.MtlsPop,
            _ => TelemetryTokenType.Unspecified,
        };
}
```

---

## Extensibility

| Need | Where to add | Interface changes? |
|------|-------------|-------------------|
| New data from MSAL to scheme | Add property to `TokenAcquisitionContext` | ❌ No |
| New output from scheme to MSAL | Add property to `TokenRequestDescriptor` | ❌ No |
| New caller feature flag | Add value to `TokenRequestFlags` | ❌ No |
| New telemetry type | Add value to `TelemetryTokenType` | ❌ No |
| New credential capability | Add property/enum to `CredentialRequirements` | ❌ No |
| New access token kind | Add value to `AccessTokenKind` | ❌ No |
| New lifecycle phase | Add method to `ITokenAcquisitionScheme` | ✅ Yes (rare) |

---

## MSAL Code Changes

| File | Change |
|------|--------|
| `AuthScheme/ITokenAcquisitionScheme.cs` | New interface |
| `AuthScheme/TokenAcquisitionSchemeMetadata.cs` | Static metadata |
| `AuthScheme/CredentialRequirements.cs` | Requirements + enums |
| `AuthScheme/TokenAcquisitionContext.cs` | Input object |
| `AuthScheme/TokenRequestDescriptor.cs` | Output object |
| `AuthScheme/TokenAcquisitionSchemeDefinition.cs` | Metadata + factory pair |
| `AuthScheme/MsalTokenAcquisitionSchemeDefinitions.cs` | Pre-built definitions + `FromProtocol` |
| `AuthScheme/MsalTokenAcquisitionSchemes.cs` | Low-level factory methods |
| `AuthScheme/LegacySchemeAdapter.cs` | Wraps IAuthenticationOperation/2/3 |
| `AuthScheme/Schemes/BearerScheme.cs` | Built-in |
| `AuthScheme/Schemes/BearerOverMtlsScheme.cs` | Built-in |
| `AuthScheme/Schemes/MtlsPopScheme.cs` | Built-in |
| `TokenClient.cs` | Uses `scheme.AcceptsTokenType()` |
| `ConfidentialClientExecutor.cs` | Preflight + `scheme.ConfigureAsync()` + validates requirements |
| `CacheKeyFactory.cs` | Uses `SchemeId` + `CacheBindingKeyId` |
| `AcquireTokenForClientParameterBuilder.cs` | `WithAuthenticationScheme(Func<>)` + instance overload |

---

## Implementation Order

1. **MSAL additive API:** `ITokenAcquisitionScheme`, `TokenAcquisitionSchemeMetadata`,
   `CredentialRequirements`, `TokenEndpointTransport`, `ClientCredentialMaterialMode`,
   `TokenAcquisitionContext`, `TokenRequestDescriptor`,
   `TokenAcquisitionSchemeDefinition`, `MsalTokenAcquisitionSchemeDefinitions`,
   `WithAuthenticationScheme(Func<ITokenAcquisitionScheme>)`.

2. **MSAL built-ins:** `BearerScheme`, `BearerOverMtlsScheme`, `MtlsPopScheme`.
   Keep `WithMtlsProofOfPossession()` as a wrapper calling `CreateMtlsPop`.

3. **MSAL adapter:** `LegacySchemeAdapter` wrapping `IAuthenticationOperation/2/3`.

4. **IdWeb consumption:** Replace `IsTokenBinding` with
   `definition.Metadata.CredentialRequirements`. Replace `-tokenBinding` with
   `definition.Metadata.SchemeId`. Implement scheme-upgrade logic for
   bearer-over-mTLS. Align with PR #3887.

5. **Optional:** `WithClientCredential(ClientCredentialMaterial)` to remove IdWeb's
   builder switch.

---

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `Metadata` is required on `ITokenAcquisitionScheme` (not optional side interface) | IdWeb must ask every scheme what it needs. Optional means fallback inference logic persists. |
| `ConfigureAsync` is `ValueTask` (not void) | `IAuthenticationOperation3.AfterCredentialEvaluationAsync` is already async. Future-proofs without needing `ITokenAcquisitionScheme2`. |
| `ConfigureAsync` runs before cache lookup | Preserves `IAuthenticationOperation3` behavior. Scheme needs cert for cache key computation and result population on cache hits. |
| `ClientCredentialMaterialMode` enum instead of `RequiresBindingCertificate` boolean | Boolean "requires binding certificate" is misleading: bearer-over-mTLS requires a cert but is not "binding." Enum makes the distinction unambiguous. |
| `TokenEndpointTransport` enum (not `CredentialTransportRequirement.OAuth`) | "OAuth" is not a transport. The enum now names what it describes: `DefaultTls` vs `Mtls`. |
| Factory-based `WithAuthenticationScheme(Func<>)` as primary API | Prevents accidental reuse of stateful scheme instances. |
| `TokenAcquisitionSchemeDefinition` pairs metadata + factory | IdWeb reads `definition.Metadata` before credential resolution, passes `definition.Create` to MSAL later. Clean separation of concerns. |
| `MsalTokenAcquisitionSchemeDefinitions` is the IdWeb-facing shape | Single consistent API. IdWeb never creates/captures scheme instances directly. |
| Factory methods (`CreateMtlsPop()`), not singleton properties | Schemes store state (`_cert`). Factory naming makes per-acquisition lifecycle obvious. |
| MSAL invokes factory exactly once per `ExecuteAsync` | Same instance flows through preflight → configure → cache → request → format. Prevents double-factory bugs. |
| `BearerOverMtlsScheme` does NOT set `BindingCertificate` | The cert is for client auth to Entra, not for sender-constraining the downstream access token. |
| `TokenRequestDescriptor` is fully immutable (constructor-based) | Clean API hygiene. No casts needed. |
| `CacheBindingKeyId` instead of `KeyId` | Name clarifies it partitions token cache only when token is key-bound. Null for bearer-over-mTLS. |
| `CredentialMaterialMode` drives MSAL's internal `CredentialTransportProtocol` (not `Transport`) | Transport = token endpoint channel. Material mode = how credential produces POST body params. |
| mTLS transport preflight is independent of material mode | Bearer-over-mTLS needs transport cert for HttpClient selection even though material mode is OAuth. |
| Requirements are static at construction | Dynamic requirements reintroduce circular dependency. Different mode = different scheme instance. |
| MSAL validates requirements at execution time | Host-side selection is for UX. MSAL-side validation is safety net. |
| `FromProtocol` fails closed for unknown strings | Typos silently downgrading to Bearer is a security risk. Only null/empty defaults to Bearer. |
| Built-ins throw `MsalClientException` | Consistent with MSAL's error taxonomy. Proper error codes enable diagnostics. |
| Credential ordering remains primary selection mechanism | Bound upgrade only fires if the *selected* credential (by order) requires bound presentation. |
| Constructor-based public types (not `required` properties) | Avoids target-framework compatibility issues. |

---

## Team Feedback

### Bogdan: "Main issue is coordination between operations"

The scheme is a single class that owns all coordination. Cache keys, token types,
result formatting, and credential requirements are co-located.

### trwalke: "Too many moving components"

One interface replaces three (`IAuthenticationOperation/2/3`). Extension is additive
(add properties to context/descriptor) not multiplicative (new interfaces).

### trwalke: "`IAuthenticationScheme` already used in MISE/IdWeb"

`ITokenAcquisitionScheme` avoids confusion with ASP.NET Core's `IAuthenticationScheme`.

---

## Open Questions (Resolved)

| # | Question | Resolution |
|---|----------|------------|
| 1 | Should `WithAuthenticationScheme()` be on all builders or only `AcquireTokenForClient`? | Start with `AcquireTokenForClient` and managed identity. Add OBO/silent later. |
| 2 | Should MSAL validate credential requirements? | Yes. IdWeb selects credentials; MSAL fails fast if requirements are violated. |
| 3 | Can requirements be dynamic? | No. Static at construction. Different mode = different scheme instance. |
| 4 | Do we need a `SchemeFactory` in MSAL? | Yes: `CreateBearer()`, `CreateBearerOverMtls()`, `CreateMtlsPop()`. IdWeb owns protocol-to-scheme mapping via definitions. |
| 5 | How should managed identity adopt this? | Same abstraction, separate builder path. MI shares `MtlsPopScheme` and cache semantics but not CCA credential APIs. |

---

## Design Review Summary

This proposal replaces the `IAuthenticationOperation/2/3` interface-growth pattern
with a single per-acquisition `ITokenAcquisitionScheme`. The key addition is static
scheme metadata, which lets IdWeb resolve compatible credentials before wiring MSAL.
Runtime token semantics remain owned by MSAL through `TokenRequestDescriptor`.
Bearer, bearer-over-mTLS, and mTLS PoP are modeled as separate schemes so token
endpoint transport, credential material mode, and access token kind do not get
conflated.

**Ownership boundary:**
- MSAL owns: token semantics, cache semantics, token type validation, result formatting.
- IdWeb owns: credential resolution, CCA lifecycle.
- `TokenAcquisitionSchemeDefinition.Metadata` is the seam between them.

---

## Acceptance Criteria

- [ ] `ITokenAcquisitionScheme` with `Metadata`, `ConfigureAsync` (ValueTask), `CreateTokenRequestDescriptor`, `FormatResultAsync`, `ValidateCachedTokenAsync`, `AcceptsTokenType`
- [ ] `TokenAcquisitionSchemeMetadata` (constructor-based) with `SchemeId`, `AuthorizationHeaderPrefix`, `ExpectedTokenType`, `CredentialRequirements`
- [ ] `CredentialRequirements` (constructor-based) with `Transport` (TokenEndpointTransport), `AccessTokenKind`, `AllowedCredentialKinds`, `CredentialMaterialMode`
- [ ] `TokenEndpointTransport` enum: `DefaultTls`, `Mtls`
- [ ] `ClientCredentialMaterialMode` enum: `OAuth`, `MtlsBinding`
- [ ] `TokenAcquisitionContext` with `MtlsCertificate`, `ClientId`, `Authority`, `RequestedFlags`
- [ ] `TokenRequestDescriptor` (constructor-based, immutable) with `SchemeId`, `AuthorizationHeaderPrefix`, `ExpectedTokenType`, `CacheBindingKeyId`, `TokenRequestParameters`, `AdditionalCacheParameters`, `TelemetryTokenType`
- [ ] `TokenAcquisitionSchemeDefinition` with `Metadata` + `Create` factory
- [ ] `MsalTokenAcquisitionSchemeDefinitions` with `Bearer`, `BearerOverMtls`, `MtlsPop`, `FromProtocol`
- [ ] `LegacySchemeAdapter` wrapping `IAuthenticationOperation/2/3` (no sync-over-async, full telemetry mapping: Bearer/Pop/SshCert/External/Extension/MtlsPop)
- [ ] Built-in schemes: `BearerScheme`, `BearerOverMtlsScheme` (no BindingCertificate, OAuth material mode), `MtlsPopScheme` (MtlsBinding material mode)
- [ ] Factory methods: `CreateBearer()`, `CreateBearerOverMtls()`, `CreateMtlsPop()`
- [ ] `WithAuthenticationScheme(Func<ITokenAcquisitionScheme>)` as primary public API
- [ ] Instance overload `WithAuthenticationScheme(ITokenAcquisitionScheme)` for advanced use
- [ ] `WithMtlsProofOfPossession()` becomes wrapper calling `CreateMtlsPop`
- [ ] MSAL invokes scheme factory exactly once per `ExecuteAsync` and reuses the same instance throughout the request
- [ ] MSAL validates requirements at execution time (fail-fast with `MsalClientException`)
- [ ] MSAL performs mTLS transport certificate preflight before cache lookup
- [ ] `ConfigureAsync` runs before cache lookup and before network request
- [ ] `BearerOverMtlsScheme` uses Mtls token endpoint transport but OAuth credential material mode
- [ ] `MtlsPopScheme` uses Mtls token endpoint transport and MtlsBinding credential material mode
- [ ] Bearer-over-mTLS signed assertion path: access token is Bearer, token endpoint uses mTLS, BindingCertificate is null
- [ ] mTLS PoP cache-hit path still populates BindingCertificate
- [ ] Unknown protocol strings in `FromProtocol` fail closed (throw)
- [ ] Bearer-over-mTLS scheme selection is credential-driven, not protocol-string-driven
- [ ] IdWeb consumes `TokenAcquisitionSchemeDefinition` for pre-resolution metadata and passes `definition.Create` to MSAL
- [ ] Public API avoids `required` properties for target-framework compatibility
- [ ] Factory samples do not capture and reuse a stateful scheme instance
- [ ] Bearer-over-mTLS certificate credential path preserves today's `SendCertificateOverMtls=true` behavior, either through transition wiring or through `TokenEndpointTransport.Mtls`
- [ ] `TokenRequestDescriptor` defensively copies token request parameters and additional cache parameters; callers cannot mutate descriptor contents after construction
- [ ] mTLS PoP works end-to-end via `MtlsPopScheme`
- [ ] All existing tests pass unchanged (legacy adapter)
- [ ] No breaking changes
