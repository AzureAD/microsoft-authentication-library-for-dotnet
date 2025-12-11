# MSI v2 — Key Management

## Overview

In MSI v2, before MSAL can call the managed identity endpoints (for example the Instance Metadata Service (IMDS) or the ESTS token service), it needs an asymmetric key pair. The private key is the local root of trust used for all subsequent operations (CSR generation, certificate issuance, Proof-of-Possession (PoP) binding, and mutual TLS (mTLS) handshakes).

## Key responsibilities

The key management layer is responsible for:

- Generating the RSA key pair.
- Storing and managing the private key.  
  - On Windows, using the most secure available platform keystore (KeyGuard or TPM/KSP).  
  - On Linux, keeping the key in process memory only.
- Protecting the key to the maximum capability of the platform, preferring hardware-backed and isolated storage when available.
- Providing the key for signing (CSR, PoP requests, mTLS handshakes).
- Preventing export of hardware- or KeyGuard-backed keys (no API returns or serializes the private key material).

## Key selection priority

MSAL implements a hierarchical key-provider strategy.

On **Windows**:

1. KeyGuard (preferred, especially for PoP).
2. Hardware / TPM / KSP (fallback).
3. In-memory RSA (last resort).

On **Linux**, the only available option is in-memory RSA.

### KeyGuard (preferred for PoP)

- Requires Virtualization-Based Security (VBS).
- Keys are isolated in a secure enclave.
- Provides the strongest guarantee that the private key cannot be exfiltrated.
- Preferred provider for PoP tokens on supported hosts.

### Hardware / TPM / KSP (fallback)

- Keys are backed by the Trusted Platform Module (TPM) or the Platform Crypto Provider.
- Non-exportable and tied to the device.
- Provides strong hardware-based protection, but without the additional VBS isolation offered by KeyGuard.

### In-memory RSA (last resort on Windows and only option on Linux)

- Keys are created and held in process memory.
- Software-only protection; weakest in terms of resistance to host compromise.
- Used only when stronger platform protections (KeyGuard or TPM/KSP) are unavailable.

## Key selection flow

```mermaid
sequenceDiagram
    autonumber
    participant MSAL
    participant KeyProvider as Key Provider

    MSAL->>KeyProvider: GetOrCreateKey()
    alt Cached key exists
        KeyProvider-->>MSAL: Return cached key
    else No cached key
        KeyProvider-->>KeyProvider: Acquire semaphore
        alt KeyGuard available
            KeyProvider-->>MSAL: KeyGuard key (preferred)
        else KeyGuard not available, Hardware/TPM available
            KeyProvider-->>MSAL: Hardware key
        else KeyGuard and Hardware/TPM not available
            KeyProvider-->>MSAL: In-memory RSA key
        end
        KeyProvider-->>KeyProvider: Cache key & release semaphore
    end
