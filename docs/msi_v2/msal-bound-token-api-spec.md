# MSAL .NET API Spec: `WithMtlsPopFallback()` + `MtlsPopOptions` — Bound Token Acquisition with Fallback

**Status:** Draft  
**Date:** April 30, 2026  
**Applies to:** `Microsoft.Identity.Client` (MSAL .NET)  
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
1. **Know about attestation** as a binding mechanism — a low-level implementation detail.
2. **Take a package dependency** on `Microsoft.Identity.Client.KeyAttestation`.
3. **Hard-code the binding strategy** — no fallback if attestation fails.

### Current MSAL Behavior When Things Go Wrong

| Scenario | Current Behavior | Desired Behavior |
|----------|-----------------|------------------|
| KeyGuard key + attestation succeeds | ✅ Works | ✅ Same |
| KeyGuard key + attestation provider not configured | ✅ Non-attested flow | ✅ Same |
| KeyGuard key + attestation **fails** (exception) | ❌ **Throws `attestation_failed`** | 🔄 Fall back to non-attested flow |
| Non-KeyGuard key (Hardware/InMemory) | ❌ **Throws `mtls_pop_requires_keyguard`** | 🔄 Proceed with non-attested mTLS PoP |
| mTLS PoP not supported (IMDSv1 host) | ❌ Throws | ❌ Throws (correct — no fallback to bearer) |

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
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
            this AcquireTokenForManagedIdentityParameterBuilder builder,
            MtlsPopOptions options)
        {
            if (!DesktopOsHelper.IsWindows())
            {
                throw new MsalClientException(
                    MsalError.MtlsNotSupportedForManagedIdentity,
                    MsalErrorMessage.MtlsNotSupportedForNonWindowsMessage);
            }

            builder.CommonParameters.IsMtlsPopRequested = true;
            builder.CommonParameters.IsBoundTokenFallbackEnabled = options.EnableFallback;
            return builder;
        }

        // Existing API — unchanged, strict, no fallback.
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
        /// fails (provider not configured, attestation exception, or KeyGuard unavailable),
        /// silently fall back to non-attested mTLS PoP binding.
        ///
        /// When <c>false</c> (default), MSAL uses strict mode: attestation failures
        /// and missing KeyGuard keys result in exceptions.
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
        /// Call after WithMtlsPopFallback() or WithMtlsProofOfPossession().
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

## 3. New Internal Property

Add to `AcquireTokenCommonParameters`:

```csharp
internal class AcquireTokenCommonParameters
{
    // Existing
    public bool IsMtlsPopRequested { get; set; }
    public Func<...> AttestationTokenProvider { get; set; }

    // NEW — enables the fallback chain when set via WithMtlsPopFallback()
    //        or WithMtlsProofOfPossession(new MtlsPopOptions { EnableFallback = true })
    public bool IsBoundTokenFallbackEnabled { get; set; }
}
```

Propagate to `AcquireTokenForManagedIdentityParameters`:

```csharp
internal class AcquireTokenForManagedIdentityParameters
{
    public bool IsMtlsPopRequested { get; set; }
    public bool IsBoundTokenFallbackEnabled { get; set; }  // NEW
    public Func<...> AttestationTokenProvider { get; set; }
}
```

---

## 4. Fallback Chain Logic in `ImdsV2ManagedIdentitySource`

### 4.1 Key Type Validation (Replace Throw with Fallback)

**Current code** (`CreateRequestAsync`, line ~350):
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
    if (!parameters.IsBoundTokenFallbackEnabled)
    {
        // Explicit WithMtlsProofOfPossession() — strict mode, throw as before
        throw new MsalClientException(
            "mtls_pop_requires_keyguard",
            $"mTLS PoP requires KeyGuard keys. Current key type: {keyInfo.Type}");
    }

    // WithMtlsPopFallback() — proceed with whatever key type is available
    _requestContext.Logger.Info(
        $"[ImdsV2] KeyGuard not available (key type: {keyInfo.Type}). " +
        "Proceeding with non-attested mTLS PoP binding.");
}
```

### 4.2 Attestation Failure (Replace Throw with Fallback)

**Current code** (`GetAttestationJwtAsync`, line ~498):
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
        // Explicit WithMtlsProofOfPossession() + WithAttestationSupport()
        // — strict mode, throw as before
        throw new MsalClientException(
            "attestation_failed",
            $"[ImdsV2] Attestation token provider failed: {ex.Message}",
            ex);
    }

    // WithMtlsPopFallback() — swallow attestation failure, proceed without attestation
    _requestContext.Logger.Warning(
        $"[ImdsV2] Attestation failed ({ex.Message}). " +
        "Falling back to non-attested mTLS PoP flow.");
    return null;  // null attestation JWT → non-attested flow
}
```

### 4.3 Full Fallback Chain (Summary)

When `WithMtlsPopFallback()` (or `WithMtlsProofOfPossession(new MtlsPopOptions { EnableFallback = true })`) is used, MSAL executes the following chain:

```
┌─────────────────────────────────────────────────────────────────┐
│  WithMtlsPopFallback() + WithAttestationSupport()              │
│                                                                 │
│  1. Get/create key from key provider                            │
│     ├── KeyGuard available?                                     │
│     │   ├── YES → Try attested flow                             │
│     │   │         ├── Attestation succeeds → Use attested JWT   │
│     │   │         └── Attestation fails → Log warning,          │
│     │   │                                  proceed non-attested │
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
- **Before:** IdWeb says _"use mTLS PoP **with** attestation"_ (prescriptive — knows the mechanism, no fallback)
- **After:** IdWeb says _"get me a bound token with fallback"_ + _"I have attestation capability available"_ (declarative)
- `WithAttestationSupport()` is now purely a **capability registration** ("I have the native DLL"), not a strategy directive
- If attestation fails at runtime, MSAL falls back transparently — IdWeb never sees the failure
- `WithMtlsProofOfPossession()` (no args) remains available as the strict, no-fallback API

---

## 6. Telemetry & Logging

### New Log Messages

| Level | Message | When |
|-------|---------|------|
| Info | `[ImdsV2] WithMtlsPopFallback: KeyGuard not available (key type: {type}). Using non-attested binding.` | Key provider returns non-KeyGuard key |
| Warning | `[ImdsV2] WithMtlsPopFallback: Attestation failed ({message}). Falling back to non-attested binding.` | Attestation delegate throws |
| Info | `[ImdsV2] WithMtlsPopFallback: Attestation not configured. Using non-attested binding.` | No `WithAttestationSupport()` called |
| Info | `[ImdsV2] WithMtlsPopFallback: Attested binding succeeded.` | Full attested flow completed |

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

---

## 7. Cache Key Partitioning

The current cache key includes an attestation tag (`#att=0` / `#att=1`). With fallback, a request may start as attested and fall back to non-attested. 

**Strategy:** Partition by **actual outcome**, not intent. After the binding is resolved:

```csharp
// Use the actual attestation outcome for the cache key
string attestationTag = (attestationJwt != null) ? AttestationTagEnabled : AttestationTagDisabled;
return baseKey + attestationTag;
```

This is already the effective behavior (attestation JWT is resolved before caching), so **no cache key changes needed**.

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

4. **Future options:** `MtlsPopOptions` is extensible. Future properties could include things like preferred key type, attestation timeout, or custom fallback policies.
