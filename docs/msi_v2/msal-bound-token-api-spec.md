# MSAL.NET API Spec: `WithMtlsPopFallback()` + `MtlsPopOptions` — Bound Token Acquisition with Fallback

**Status:** Draft  
**Date:** April 30, 2026  
**Applies to:** `Microsoft.Identity.Client` (MSAL.NET)  
**Related PR:** [AzureAD/microsoft-identity-web#3773](https://github.com/AzureAD/microsoft-identity-web/pull/3773)

---

## 1. Problem Statement

### Current State (PR #3773)

IdWeb currently calls MSAL's low-level APIs explicitly:

```csharp
// Pure MSI path (TokenAcquisition.cs)
miBuilder.WithMtlsProofOfPossession()
         .WithAttestationSupport();

// FIC path (ManagedIdentityClientAssertion.cs)
miBuilder.WithMtlsProofOfPossession()
         .WithAttestationSupport();
```

This requires IdWeb (a higher-level SDK) to:
1. **Orchestrate fallback policy** — IdWeb hard-codes the binding approach with no fallback if attestation fails at runtime.
2. **Take a package dependency** on `Microsoft.Identity.Client.KeyAttestation` (this dependency remains, but the fallback orchestration moves to MSAL).
3. **Couple to low-level mechanism details** — IdWeb explicitly chains `WithMtlsProofOfPossession().WithAttestationSupport()`, prescribing the exact binding strategy rather than declaring intent.

> **Note:** The `Microsoft.Identity.Client.KeyAttestation` package dependency and the `WithAttestationSupport()` call remain in IdWeb because that call brings in the native Credential Guard DLL. This proposal moves the **fallback orchestration** into MSAL, not the package dependency itself.

### Current MSAL Behavior When Things Go Wrong

| Scenario | Current Behavior | Desired Behavior |
|----------|-----------------|------------------|
| KeyGuard key + attestation succeeds | ✅ Works | ✅ Same |
| KeyGuard key + attestation provider not configured | ✅ Non-attested flow (returns null) | ✅ Same (not a fallback scenario) |
| KeyGuard key + attestation **fails** (provider throws) | ❌ **Throws `attestation_failed`** | 🔄 Fall back to non-attested flow |
| KeyGuard key + key is not RSACng | ❌ **Throws `credential_guard_requires_cng`** | 🔄 Fall back to non-attested flow |
| Non-KeyGuard key (Hardware/InMemory) | ❌ **Throws `mtls_pop_requires_keyguard`** | 🔄 Proceed with non-attested mTLS PoP |
| mTLS PoP not supported (IMDSv1 host) | ❌ Throws | ❌ Throws (correct — no fallback to bearer) |
| Non-Windows or NET462 | ❌ Throws | ❌ Throws (platform unsupported) |

### Design Principle

> **IdWeb needs a bound token. What MSAL does internally to get it should not be visible to IdWeb.**
>
> MSAL should try attested flow first, and if that fails, fall back to non-attested flow. The fallback is transparent to the caller.

---

## 2. Proposed API

Three-level API surface — from simplest to most configurable:

| API | Use Case | Fallback? |
|-----|----------|-----------|
| `WithMtlsPopFallback()` | **IdWeb / higher-level SDKs** — recommended | ✅ Yes |
| `WithMtlsProofOfPossession(MtlsPopOptions)` | Advanced callers needing fine-grained control | Configurable via options |
| `WithMtlsProofOfPossession()` | Existing strict API — no fallback | ❌ No |

### 2.1 New Convenience Method: `WithMtlsPopFallback()`

**Package:** `Microsoft.Identity.Client` (core package)  
**Target class:** `AcquireTokenForManagedIdentityParameterBuilder`

```csharp
namespace Microsoft.Identity.Client
{
    public static class ManagedIdentityPopExtensions
    {
        /// <summary>
        /// Requests an mTLS-bound (Proof-of-Possession) token with automatic fallback.
        /// MSAL will first attempt the attested binding flow (if WithAttestationSupport()
        /// was called). If attestation fails, MSAL silently falls back to the
        /// non-attested mTLS PoP flow instead of throwing.
        ///
        /// This is the recommended API for higher-level SDKs (e.g., Microsoft.Identity.Web)
        /// that need a bound token without coupling to specific binding mechanisms.
        ///
        /// Equivalent to:
        ///   WithMtlsProofOfPossession(new MtlsPopOptions { EnableFallback = true })
        /// </summary>
        /// <param name="builder">The AcquireTokenForManagedIdentityParameterBuilder instance.</param>
        /// <returns>The builder to chain .With methods.</returns>
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsPopFallback(
            this AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            return builder.WithMtlsProofOfPossession(
                new MtlsPopOptions { EnableFallback = true });
        }
    }
}
```

### 2.2 New Overload: `WithMtlsProofOfPossession(MtlsPopOptions)`

```csharp
namespace Microsoft.Identity.Client
{
    public static class ManagedIdentityPopExtensions
    {
        /// <summary>
        /// Enables mTLS Proof-of-Possession with configurable behavior.
        /// Use <see cref="MtlsPopOptions"/> to control fallback and other settings.
        /// </summary>
        /// <param name="builder">The AcquireTokenForManagedIdentityParameterBuilder instance.</param>
        /// <param name="options">Options controlling mTLS PoP behavior.</param>
        /// <returns>The builder to chain .With methods.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
            this AcquireTokenForManagedIdentityParameterBuilder builder,
            MtlsPopOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

#if NET462
            throw new MsalClientException(
                MsalError.MtlsNotSupportedForManagedIdentity,
                MsalErrorMessage.MtlsNotSupportedForManagedIdentityMessage);
#else
            if (!DesktopOsHelper.IsWindows())
            {
                throw new MsalClientException(
                    MsalError.MtlsNotSupportedForManagedIdentity,
                    MsalErrorMessage.MtlsNotSupportedForNonWindowsMessage);
            }

            builder.CommonParameters.IsMtlsPopRequested = true;
            builder.CommonParameters.IsBoundTokenFallbackEnabled = options.EnableFallback;
            return builder;
#endif
        }

        // Existing API — unchanged, strict, no fallback.
        // The new overload mirrors the same NET462/non-Windows constraints.
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
            this AcquireTokenForManagedIdentityParameterBuilder builder) { /* unchanged */ }
    }
}
```

### 2.3 New Options Class: `MtlsPopOptions`

```csharp
namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Options for configuring mTLS Proof-of-Possession token acquisition behavior.
    /// </summary>
    public class MtlsPopOptions
    {
        /// <summary>
        /// When <c>true</c>, MSAL will attempt attested binding first, and if attestation
        /// fails at runtime (attestation provider throws, or key is not RSACng for Credential Guard),
        /// silently fall back to non-attested mTLS PoP binding instead of throwing.
        /// Also allows non-KeyGuard key types (Hardware, InMemory) to proceed with
        /// non-attested binding instead of throwing `mtls_pop_requires_keyguard`.
        ///
        /// Note: When the attestation provider is not configured (WithAttestationSupport()
        /// not called), MSAL already proceeds with non-attested flow in both strict
        /// and fallback modes — this is existing behavior, not a fallback scenario.
        ///
        /// When <c>false</c> (default), MSAL uses strict mode: attestation provider
        /// exceptions and non-KeyGuard keys result in exceptions.
        ///
        /// Default: <c>false</c>
        /// </summary>
        public bool EnableFallback { get; set; }
    }
}
```

### 2.4 `WithAttestationSupport()` — Unchanged

`WithAttestationSupport()` stays in `Microsoft.Identity.Client.KeyAttestation` package. It **still needs to be called by IdWeb** because:
1. It brings in the **native Credential Guard DLL** via the KeyAttestation package.
2. It **registers** the attestation token provider delegate on the builder.
3. Without it, the fallback chain simply skips attestation and proceeds to non-attested binding.

```csharp
// KeyAttestation package — NO changes to this API
namespace Microsoft.Identity.Client.KeyAttestation
{
    public static class ManagedIdentityAttestationExtensions
    {
        /// <summary>
        /// Registers the Credential Guard attestation provider.
        /// Used with WithMtlsPopFallback() or WithMtlsProofOfPossession() to enable
        /// attested mTLS PoP flows. Order of calls does not matter — both set
        /// independent builder state.
        ///
        /// When used with WithMtlsPopFallback():
        ///   - Attestation is attempted first
        ///   - On failure, MSAL falls back to non-attested flow
        ///
        /// When used with WithMtlsProofOfPossession() (no options):
        ///   - Attestation failure throws an exception (strict mode)
        /// </summary>
        public static AcquireTokenForManagedIdentityParameterBuilder WithAttestationSupport(
            this AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            // Unchanged — sets AttestationTokenProvider delegate
        }
    }
}
```

---

## 3. Internal Plumbing

### 3.1 New Property on Parameter Classes

Add to `AcquireTokenCommonParameters`:

```csharp
internal class AcquireTokenCommonParameters
{
    // Existing
    public bool IsMtlsPopRequested { get; set; }
    public Func<string, SafeHandle, string, string, ILoggerAdapter, CancellationToken, Task<string>>
        AttestationTokenProvider { get; set; }

    // NEW
    public bool IsBoundTokenFallbackEnabled { get; set; }
}
```

Add to `AcquireTokenForManagedIdentityParameters`:

```csharp
internal class AcquireTokenForManagedIdentityParameters
{
    public bool IsMtlsPopRequested { get; set; }
    public bool IsBoundTokenFallbackEnabled { get; set; }  // NEW
    public Func<string, SafeHandle, string, string, ILoggerAdapter, CancellationToken, Task<string>>
        AttestationTokenProvider { get; set; }
}
```

### 3.2 Propagation in `ApplyMtlsPopAndAttestation()`

The existing `ApplyMtlsPopAndAttestation()` in `AcquireTokenForManagedIdentityParameterBuilder` copies `IsMtlsPopRequested` and `AttestationTokenProvider` from common params to MI params. It must also copy the new flag:

```csharp
private static void ApplyMtlsPopAndAttestation(
    AcquireTokenCommonParameters acquireTokenCommonParameters,
    AcquireTokenForManagedIdentityParameters acquireTokenForManagedIdentityParameters)
{
    acquireTokenForManagedIdentityParameters.IsMtlsPopRequested =
        acquireTokenCommonParameters.IsMtlsPopRequested;
    acquireTokenForManagedIdentityParameters.AttestationTokenProvider =
        acquireTokenCommonParameters.AttestationTokenProvider;

    // NEW — propagate fallback flag
    acquireTokenForManagedIdentityParameters.IsBoundTokenFallbackEnabled =
        acquireTokenCommonParameters.IsBoundTokenFallbackEnabled;

    // existing cache key partitioning...
}
```

### 3.3 Capture in `ImdsV2ManagedIdentitySource`

The `ImdsV2ManagedIdentitySource` already captures `_attestationTokenProvider` from `parameters.AttestationTokenProvider` in its `AuthenticateAsync()` method. The new flag must be captured the same way:

```csharp
internal class ImdsV2ManagedIdentitySource : AbstractManagedIdentity
{
    private Func<...> _attestationTokenProvider;
    private bool _isBoundTokenFallbackEnabled;  // NEW

    public override async Task<ManagedIdentityResponse> AuthenticateAsync(
        AcquireTokenForManagedIdentityParameters parameters,
        CancellationToken cancellationToken)
    {
        _attestationTokenProvider = parameters.AttestationTokenProvider;
        _isBoundTokenFallbackEnabled = parameters.IsBoundTokenFallbackEnabled;  // NEW

        // ... existing logic
    }
}
```

This ensures both `_attestationTokenProvider` and `_isBoundTokenFallbackEnabled` are available as instance fields throughout `CreateRequestAsync()`, `GetAttestationJwtAsync()`, and other methods.

---

## 4. Fallback Chain Logic in `ImdsV2ManagedIdentitySource`

### 4.1 Key Type Validation (Replace Throw with Fallback)

**Current code** (`CreateRequestAsync`):
```csharp
if (keyInfo.Type != ManagedIdentityKeyType.KeyGuard)
{
    throw new MsalClientException(
        "mtls_pop_requires_keyguard",
        $"mTLS PoP requires KeyGuard keys. Current key type: {keyInfo.Type}");
}
```

**Proposed change:**
```csharp
if (keyInfo.Type != ManagedIdentityKeyType.KeyGuard)
{
    if (!_isBoundTokenFallbackEnabled)
    {
        // Strict mode — throw as before
        throw new MsalClientException(
            "mtls_pop_requires_keyguard",
            $"mTLS PoP requires KeyGuard keys. Current key type: {keyInfo.Type}");
    }

    // Fallback mode — remove the early gate, let the existing non-attested
    // path in ExecuteCertificateRequestAsync() handle non-KeyGuard keys
    _requestContext.Logger.Info(
        $"[ImdsV2] KeyGuard not available (key type: {keyInfo.Type}). " +
        "Proceeding with non-attested mTLS PoP binding.");
}
```

### 4.2 Attestation Failure (Replace Throw with Fallback)

**Current code** (`GetAttestationJwtAsync`):
```csharp
catch (Exception ex)
{
    throw new MsalClientException(
        "attestation_failed",
        $"[ImdsV2] Attestation token provider failed: {ex.Message}",
        ex);
}
```

**Proposed change:**
```csharp
catch (Exception ex)
{
    if (!_isBoundTokenFallbackEnabled)
    {
        // Strict mode — throw as before
        throw new MsalClientException(
            "attestation_failed",
            $"[ImdsV2] Attestation token provider failed: {ex.Message}",
            ex);
    }

    // Fallback mode — swallow attestation failure, proceed without attestation
    _requestContext.Logger.Warning(
        $"[ImdsV2] Attestation failed ({ex.Message}). " +
        "Falling back to non-attested mTLS PoP flow.");
    return null;  // null attestation JWT → non-attested flow
}
```

### 4.3 CNG Key Validation (Replace Throw with Fallback)

**Current code** (`GetAttestationJwtAsync`):
```csharp
if (keyInfo.Key is not System.Security.Cryptography.RSACng rsaCng)
{
    throw new MsalClientException(
        "credential_guard_requires_cng",
        "[ImdsV2] Credential Guard attestation currently supports only RSA CNG keys on Windows.");
}
```

**Proposed change:**
```csharp
if (keyInfo.Key is not System.Security.Cryptography.RSACng rsaCng)
{
    if (!_isBoundTokenFallbackEnabled)
    {
        throw new MsalClientException(
            "credential_guard_requires_cng",
            "[ImdsV2] Credential Guard attestation currently supports only RSA CNG keys.");
    }

    _requestContext.Logger.Warning(
        "[ImdsV2] Key is not RSACng, cannot perform Credential Guard attestation. " +
        "Falling back to non-attested mTLS PoP flow.");
    return null;
}
```

> **Design decision:** Fallback mode absorbs **all attestation-stage failures** — provider exceptions, CNG key type mismatch, and non-KeyGuard keys. The only failures NOT absorbed are platform-level (non-Windows, NET462) and infrastructure-level (IMDSv1 host, IMDS unreachable).

### 4.3 Full Fallback Chain (Summary)

When `WithMtlsPopFallback()` (or `WithMtlsProofOfPossession(new MtlsPopOptions { EnableFallback = true })`) is used, MSAL executes the following chain:

```
┌─────────────────────────────────────────────────────────────────┐
│  WithMtlsPopFallback() + WithAttestationSupport()              │
│                                                                 │
│  1. Get/create key from key provider                            │
│     ├── KeyGuard available?                                     │
│     │   ├── YES → Key is RSACng?                                │
│     │   │         ├── YES → Try attested flow                   │
│     │   │         │         ├── Succeeds → Use attested JWT     │
│     │   │         │         └── Fails → Log warning,            │
│     │   │         │                      proceed non-attested   │
│     │   │         └── NO → Log warning, proceed non-attested    │
│     │   └── NO → Log info, proceed with available key type      │
│     │                                                           │
│  2. Generate CSR with available key                             │
│  3. Request certificate from IMDS V2                            │
│  4. Acquire token with mTLS binding                             │
│                                                                 │
│  Result: Best available bound token                             │
└─────────────────────────────────────────────────────────────────┘
```

```
┌─────────────────────────────────────────────────────────────────┐
│  WithMtlsPopFallback() WITHOUT WithAttestationSupport()        │
│                                                                 │
│  1. Get/create key from key provider                            │
│  2. Skip attestation (no provider registered)                   │
│  3. Generate CSR with available key                             │
│  4. Request certificate from IMDS V2                            │
│  5. Acquire token with non-attested mTLS binding                │
│                                                                 │
│  Result: Non-attested bound token                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Impact on IdWeb (PR #3773)

> **Scope note:** The FIC (Federated Identity Credential) path in IdWeb acquires a managed identity token as a client assertion. This reaches MSAL through `AcquireTokenForManagedIdentity` — the same managed identity builder. No confidential client (`AcquireTokenForClient`) API surface is changed by this proposal.

### Before (Current PR)
```csharp
// TokenAcquisition.cs — Pure MSI
if (isTokenBinding)
{
    miBuilder.WithMtlsProofOfPossession()
             .WithAttestationSupport();   // IdWeb knows about attestation
}

// ManagedIdentityClientAssertion.cs — FIC
if (IsTokenBinding)
{
    miBuilder.WithMtlsProofOfPossession()
             .WithAttestationSupport();   // IdWeb knows about attestation
}
```

### After — Option 1: `WithMtlsPopFallback()` (Recommended)
```csharp
// TokenAcquisition.cs — Pure MSI
if (isTokenBinding)
{
    miBuilder.WithMtlsPopFallback()       // "try attested, fall back to non-attested"
             .WithAttestationSupport();   // Still needed: brings in native DLL
}

// ManagedIdentityClientAssertion.cs — FIC
if (IsTokenBinding)
{
    miBuilder.WithMtlsPopFallback()       // "try attested, fall back to non-attested"
             .WithAttestationSupport();   // Still needed: brings in native DLL
}
```

### After — Option 2: `WithMtlsProofOfPossession(MtlsPopOptions)` (Equivalent, explicit)
```csharp
// TokenAcquisition.cs — Pure MSI
if (isTokenBinding)
{
    miBuilder.WithMtlsProofOfPossession(new MtlsPopOptions { EnableFallback = true })
             .WithAttestationSupport();   // Still needed: brings in native DLL
}

// ManagedIdentityClientAssertion.cs — FIC
if (IsTokenBinding)
{
    miBuilder.WithMtlsProofOfPossession(new MtlsPopOptions { EnableFallback = true })
             .WithAttestationSupport();   // Still needed: brings in native DLL
}
```

> Both options produce identical behavior. Option 1 is syntactic sugar for Option 2.

### Key Difference
- **Before:** IdWeb says _"use mTLS PoP **with** attestation"_ (prescriptive — no fallback if attestation fails)
- **After:** IdWeb says _"get me a bound token with fallback"_ + _"I have attestation capability available"_ (MSAL handles fallback)
- `WithAttestationSupport()` is now purely a **capability registration** — IdWeb still calls it (and keeps the KeyAttestation package dependency) because it brings in the native DLL, but MSAL decides the fallback policy
- If attestation fails at runtime, MSAL falls back transparently — IdWeb never sees the failure
- `WithMtlsProofOfPossession()` (no args) remains available as the strict, no-fallback API

---

## 6. Telemetry & Logging

### New Log Messages

These messages should match the exact strings used in the fallback code paths (Section 4):

| Level | Message | When |
|-------|---------|------|
| Info | `[ImdsV2] KeyGuard not available (key type: {type}). Proceeding with non-attested mTLS PoP binding.` | Key provider returns non-KeyGuard key (Section 4.1) |
| Warning | `[ImdsV2] Attestation failed ({message}). Falling back to non-attested mTLS PoP flow.` | Attestation provider delegate throws (Section 4.2) |
| Warning | `[ImdsV2] Key is not RSACng, cannot perform Credential Guard attestation. Falling back to non-attested mTLS PoP flow.` | KeyGuard key but not RSACng (Section 4.3) |
| Info | `[ImdsV2] Attestation token provider not configured. Proceeding with non-attested flow.` | No `WithAttestationSupport()` called (existing behavior, unchanged) |

### Telemetry Events

Add to `ApiEvent` or equivalent:

```csharp
public enum MtlsBindingOutcome
{
    Attested,            // Full attested KeyGuard flow
    NonAttestedKeyGuard, // KeyGuard key, attestation skipped/failed
    NonAttestedOther,    // Hardware or InMemory key
    NotRequested         // No mTLS binding requested
}
```

Log `IsBoundTokenFallbackEnabled` in `AcquireTokenForManagedIdentityParameters.LogParameters()` for debugging rollout issues.

---

## 7. Cache Key Partitioning

Two caches are affected by the attestation tag:

### 7.1 Token Cache (in `AcquireTokenForManagedIdentityParameterBuilder.ApplyMtlsPopAndAttestation()`)

Current code partitions by whether `AttestationTokenProvider` is configured:
```csharp
acquireTokenCommonParameters.CacheKeyComponents[MiAttCacheKeyComponent] =
    _ => acquireTokenCommonParameters.AttestationTokenProvider != null ? s_att1 : s_att0;
```

### 7.2 Cert Cache (in `ImdsV2ManagedIdentitySource.GetMtlsCertCacheKey()`)

Current code also partitions by provider presence:
```csharp
return baseKey + (_attestationTokenProvider != null ? AttestationTagEnabled : AttestationTagDisabled);
```

### 7.3 Problem with Fallback

With fallback enabled, the attestation provider **is** configured (so cache key = `#att=1`), but attestation may **fail** at runtime, producing a non-attested binding. This means:
- A non-attested cert/token gets cached under the `#att=1` partition
- If attestation later succeeds (transient failure), the cached non-attested artifact is returned instead of the attested one

### 7.4 Required Change

Both cache keys must be based on the **actual outcome**, not provider presence. However, the cert cache key is computed **before** attestation runs (it's used to look up cached certs). This means:

**Option A (Recommended):** When fallback is enabled, always use `#att=0` for the cache key. Attested and non-attested certs produce functionally equivalent bound tokens — the attestation only affects the trust level of the CSR, not the resulting cert's mTLS binding capability. This avoids the stale-cache problem entirely.

```csharp
// In GetMtlsCertCacheKey():
if (_isBoundTokenFallbackEnabled)
{
    return baseKey + AttestationTagDisabled; // fallback mode: single partition
}
return baseKey + (_attestationTokenProvider != null ? AttestationTagEnabled : AttestationTagDisabled);
```

**Option B:** Split the cert provisioning into lookup → attestation → cache-store, making the cache key depend on outcome. This is more complex and may not be worth it if Option A is acceptable.

The same approach applies to the token cache key component in `ApplyMtlsPopAndAttestation()`.

---

## 8. API Comparison Matrix

| Aspect | `WithMtlsProofOfPossession()` | `WithMtlsProofOfPossession(MtlsPopOptions)` | `WithMtlsPopFallback()` |
|--------|-------------------------------|----------------------------------------------|-------------------------|
| Package | `Microsoft.Identity.Client` | `Microsoft.Identity.Client` | `Microsoft.Identity.Client` |
| Sets `IsMtlsPopRequested` | ✅ | ✅ | ✅ |
| Sets `IsBoundTokenFallbackEnabled` | ❌ | Configurable | ✅ (`true`) |
| Requires KeyGuard | ✅ throws if not | Depends on `EnableFallback` | ❌ falls back |
| On attestation failure | ❌ throws | Depends on `EnableFallback` | 🔄 falls back to non-attested |
| Intended caller | Advanced / low-level | Fine-grained control | **IdWeb / higher-level SDKs** |
| Breaking change | None | None (new overload) | None (new method) |

---

## 9. Breaking Changes

**None.** This is purely additive:
- `WithMtlsProofOfPossession()` behavior is unchanged (strict, no fallback).
- `WithAttestationSupport()` behavior is unchanged.
- `WithMtlsProofOfPossession(MtlsPopOptions)` is a new overload.
- `WithMtlsPopFallback()` is a new convenience method.

---

## 10. Migration Guide

### For IdWeb — Option 1 (recommended)
```diff
- miBuilder.WithMtlsProofOfPossession()
-          .WithAttestationSupport();
+ miBuilder.WithMtlsPopFallback()
+          .WithAttestationSupport();
```

### For IdWeb — Option 2 (explicit via options)
```diff
- miBuilder.WithMtlsProofOfPossession()
-          .WithAttestationSupport();
+ miBuilder.WithMtlsProofOfPossession(new MtlsPopOptions { EnableFallback = true })
+          .WithAttestationSupport();
```

### For direct MSAL consumers who want strict behavior (no fallback)
No change needed. Continue using `WithMtlsProofOfPossession()` (no args).

---

## 11. Open Questions

1. **Should `WithMtlsPopFallback()` without `WithAttestationSupport()` log a warning?** Currently it would silently use non-attested flow. Should MSAL log an informational message that attestation capability is not registered?

2. **Should the fallback also cover IMDSv1 hosts?** Currently, if the host only supports IMDSv1 (404 from CSR metadata endpoint), the request throws. Should `WithMtlsPopFallback()` fall back to a bearer token on IMDSv1 hosts? *(Likely no — bound token is the requirement, not optional.)*

3. **Future options:** `MtlsPopOptions` is extensible. Future properties could include things like preferred key type, attestation timeout, or custom fallback policies.
