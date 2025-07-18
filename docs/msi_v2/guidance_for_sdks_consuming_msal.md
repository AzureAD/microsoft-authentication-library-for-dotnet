# Guidance for SDKs Consuming MSAL

## Overview

To support MSI V2 authentication with the new `/issuecredential` endpoint, the **Azure SDK** will leverage the `IMtlsHttpClientFactory` interface and **certificate management APIs** for secure communication with Azure AD using **mutual TLS (mTLS)**.

_This section covers:_
- How Azure SDK uses **`IMtlsHttpClientFactory`** for MTLS authentication.
- How SDKs interact with the **certificate APIs** to obtain information about binding certificate.
- The **new `CertificateRefreshed` event**, which notifies when a binding certificate is updated.

---

## **Binding Certificate**

SDKs customizing the httpclient factory will continue to use the old `IMtlsHttpClientFactory` interface. MSAL will use the customized httpclient factory to, 

- call into `platformmetadata` endpoint to form the CSR.
- Use the CSR and call into `issuecredential` endpoint to get the Certificate.
- Once. MSAL acquires a certificate, MSAL will will call the mTLS endpoint using this certificate. MSAL will not use the customized factory for this call. 

| API Name                             | Purpose                                                                            |
|--------------------------------------|------------------------------------------------------------------------------------|
| `GetManagedIdentitySourceAsync()`    | Will expose the MSI Source including the new `IMDSV2` source                       |
| `BindingCertificateRefreshed`        | Event to notify SDKs when the binding certificate is updated.                      |
| `IsPopSupported()`                   | Helper method to check if POP is supported.                                        |
| `ResetInternalStaticCachesForTest()` | Helper method to reset internal static caches.                                     |



