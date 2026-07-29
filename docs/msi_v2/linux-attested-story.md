## Linux Attested Managed Identity Flow

This flow extends MSI V2 on Linux by using KMPP to generate and retain a protected, non-exportable private key. MSAL attests that key, obtains a short-lived managed identity certificate through IMDS, and uses the certificate to acquire an mTLS-bound access token from regional ESTS.

```mermaid
sequenceDiagram
    participant Workload as 1P Workload
    participant MSAL
    participant IMDS as IMDS / Managed Identity RP
    participant KMPP as KMPP Client
    participant TA as KMPP Trusted Application
    participant MAA as Microsoft Azure Attestation
    participant ESTS as Regional Entra STS
    participant AKV as Azure Key Vault

    Workload->>MSAL: Acquire token for AKV

    MSAL->>IMDS: Probe MSI V2 support
    IMDS-->>MSAL: issuecredential supported

    MSAL->>IMDS: GET getPlatformMetadata
    IMDS-->>MSAL: client_id, tenant_id, CUID, MAA endpoint

    MSAL->>KMPP: Create or load protected binding key
    KMPP->>TA: Generate protected key
    TA-->>KMPP: Key reference and public key
    KMPP-->>MSAL: Public key and protected-key handle

    MSAL->>MAA: Request attestation token for protected key
    MAA-->>MSAL: Attestation token

    MSAL->>IMDS: POST issuecredential<br/>protected-key handle and attestation token

    Note over IMDS,KMPP: IMDS internally creates and signs the CSR<br/>using the protected KMPP key

    IMDS-->>MSAL: X.509 certificate<br/>regional_token_url and expiry metadata

    MSAL->>ESTS: Token request over mTLS<br/>token_type=mtls_pop
    ESTS-->>MSAL: Certificate-bound access token

    MSAL-->>Workload: Access token and mTLS certificate

    Workload->>AKV: Request secret over mTLS
    AKV-->>Workload: Protected secret
```
