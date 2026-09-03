# Guidance for SDKs Consuming MSAL

## Overview

To support MSI V2 authentication with the new `/issuecredential` endpoint, the **Azure SDK** will leverage the `IMsalHttpClientFactory` interface and **certificate management APIs** for secure communication with Azure AD using **mutual TLS (mTLS)**.

_This section covers:_
- How Azure SDK uses **`IMsalHttpClientFactory`** for MTLS authentication.
- How SDKs interact with the **certificate APIs** to obtain information about binding certificate.
- The **new `CertificateRefreshed` event**, which notifies when a binding certificate is updated.

---

## **Binding Certificate**

SDKs customizing the httpclient factory will continue to use the old `IMsalHttpClientFactory` interface. MSAL will use the customized httpclient factory to, 

- call into `platformmetadata` endpoint to form the CSR.
- Use the CSR and call into `issuecredential` endpoint to get the Certificate.
- Once. MSAL acquires a certificate, MSAL will will call the mTLS endpoint using this certificate. MSAL will not use the customized factory for this call. 

| API Name                             | Purpose                                                                            |
|--------------------------------------|------------------------------------------------------------------------------------|
| `GetManagedIdentitySourceAsync()`    | Will expose the MSI Source including the new `IMDSV2` source                       |
| `BindingCertificateRefreshed`        | Event to notify SDKs when the binding certificate is updated.                      |
| `IsPopSupported()`                   | Helper method to check if POP is supported.                                        |
| `ResetInternalStaticCachesForTest()` | Helper method to reset internal static caches.                                     |

---

## Capability discovery and the IMDSv2 kill switch

IMDSv2 can be disabled for a process by setting the `MSAL_MI_DISABLE_IMDS_V2` environment
variable. While it is set, `GetManagedIdentityCapabilitiesAsync()` reports
`MaxSupportedBindingStrength = None` and `IsMtlsPopSupportedByHost = false`, and mTLS requests
throw rather than returning an unbound token.

No SDK change is required. The existing guidance to branch on capability rather than on the source
label already produces the correct behavior: the capabilities API reports what the caller can
actually obtain, so a chain such as `DefaultAzureCredential` selects the bearer path instead of a
PoP path that would fail on every request.

The variable is read from the process environment, which no external actor can modify while the
process runs, so the reported capability is stable for the lifetime of the process and does not need
to be re-checked. See [IMDSv2 Kill Switch](./imds_v2_kill_switch.md) for details.



