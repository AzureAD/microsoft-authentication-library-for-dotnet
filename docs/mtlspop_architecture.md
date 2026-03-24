# MTLSPoP (Managed Identity) – Architecture Diagram

## Full Token Acquisition Flow

```mermaid
sequenceDiagram
    autonumber
    participant App as Your Application
    participant MSAL as MSAL.NET
    participant MIClient as ManagedIdentityClient
    participant IMDSv2 as ImdsV2ManagedIdentitySource
    participant CertCache as MtlsCertificateCache<br/>(Memory + Windows Store)
    participant IMDS as Azure IMDS<br/>(169.254.169.254)
    participant MAA as Microsoft Azure Attestation<br/>(MAA)
    participant STS as Regional STS<br/>(mtlsauth.microsoft.com)

    App->>MSAL: AcquireTokenForManagedIdentity(resource)<br/>.WithMtlsProofOfPossession()<br/>.ExecuteAsync()

    Note over MSAL: ManagedIdentityAuthRequest.ExecuteAsync()<br/>Check token cache first

    alt Token is in MSAL token cache (not expired)
        MSAL-->>App: Return cached AuthenticationResult<br/>(token_type=mtls_pop, BindingCertificate)
    else Cache miss — need a fresh token
        MSAL->>MIClient: SendTokenRequestForManagedIdentityAsync()
        MIClient->>IMDSv2: Route (mTLS PoP → always IMDSv2)

        rect rgb(230, 240, 255)
            Note over IMDSv2,IMDS: Step A — Get Platform Metadata
            IMDSv2->>IMDS: GET /metadata/identity/getplatformmetadata<br/>?cred-api-version=2.0<br/>Header: Metadata: true
            IMDS-->>IMDSv2: CsrMetadata<br/>{ clientId, tenantId, cuId, attestationEndpoint }
        end

        rect rgb(230, 255, 230)
            Note over IMDSv2,CertCache: Step B — Get or Mint Binding Certificate
            IMDSv2->>CertCache: GetOrCreateMtlsBindingAsync(cacheKey)

            alt Certificate is cached (memory or Windows store)
                CertCache-->>IMDSv2: MtlsBindingInfo<br/>{ Certificate, Endpoint, ClientId }
            else Cache miss — mint a new cert
                Note over IMDSv2: GetOrCreateKeyAsync() → KeyGuard RSA key
                Note over IMDSv2: Csr.Generate(key, clientId, tenantId, cuId)<br/>→ CSR (PEM) + private key

                opt KeyGuard key type + WithAttestationSupport() configured
                    Note over IMDSv2: GetAttestationJwtAsync()<br/>calls AttestationClientLib.dll (native)
                    IMDSv2->>MAA: POST {csrMetadata.attestationEndpoint}/attest/keyguard<br/>TPM logs + VBS evidence
                    MAA-->>IMDSv2: MAA JWT (proves key is hardware-backed)
                end

                IMDSv2->>IMDS: POST /metadata/identity/issuecredential<br/>Body: { csr: "&lt;raw base64, PEM headers stripped&gt;", attestation_token: MAA JWT or omitted }
                IMDS-->>IMDSv2: CertificateRequestResponse<br/>{ certificate (Base64 X.509),<br/>mtls_authentication_endpoint,<br/>client_id, tenant_id }

                Note over IMDSv2: Attach private key to certificate<br/>Store in memory + Windows cert store
                IMDSv2->>CertCache: Store MtlsBindingInfo
                CertCache-->>IMDSv2: MtlsBindingInfo<br/>{ Certificate, Endpoint, ClientId }
            end
        end

        rect rgb(255, 245, 220)
            Note over IMDSv2,STS: Step C — Acquire mTLS PoP Token
            IMDSv2->>STS: POST {regional_endpoint}/{tenantId}/oauth2/v2.0/token<br/>🔒 mTLS — binding certificate used for TLS handshake<br/>Body: { client_id, grant_type=client_credentials,<br/>scope=resource/.default, token_type=mtls_pop }
            STS-->>IMDSv2: Token response<br/>{ access_token, token_type="mtls_pop", expires_in }
        end

        Note over MSAL: Apply MtlsPopAuthenticationOperation<br/>Cache token (keyed by scope + attestation mode)<br/>Set AuthenticationResult.BindingCertificate
        MSAL-->>App: AuthenticationResult<br/>{ AccessToken (mtls_pop), BindingCertificate,<br/>TokenType="mtls_pop", ExpiresOn }
    end
```

---

## Component Relationships

```mermaid
graph TD
    A["🔵 Public API<br/>ManagedIdentityPopExtensions<br/>.WithMtlsProofOfPossession()"] --> B

    B["AcquireTokenForManagedIdentityParameterBuilder<br/>Sets IsMtlsPopRequested = true"] --> C

    C["ManagedIdentityAuthRequest<br/>Orchestrates cache lookup + token acquisition"] --> D

    D["ManagedIdentityClient<br/>Source selection & routing<br/>Holds runtime binding certificate"] -->|mTLS PoP → always IMDSv2| E

    E["ImdsV2ManagedIdentitySource<br/>Full CSR → Cert → Token flow"] --> F
    E --> G
    E --> H

    F["IMDS /getplatformmetadata<br/>Gets CsrMetadata<br/>(clientId, tenantId, cuId)"]
    
    G["MtlsBindingCache<br/>2-tier: Memory + Windows Store"] -->|cache miss| I
    G -->|cache hit| J

    I["CSR + Certificate Issuance<br/>IMDS /issuecredential<br/>Returns signed X.509 cert"]
    J["Cached MtlsBindingInfo<br/>(cert + endpoint + clientId)"]

    H["MtlsPopAuthenticationOperation<br/>Sets token_type=mtls_pop<br/>Sets BindingCertificate on result"] --> K

    K["Regional STS Token Request<br/>🔒 mTLS connection with binding cert<br/>Returns mtls_pop token"]

    style A fill:#4a90d9,color:#fff
    style K fill:#27ae60,color:#fff
    style G fill:#f39c12,color:#fff
```

---

## Certificate Cache Architecture

```mermaid
graph LR
    Request["Token Request"] --> Check1

    Check1{"In-memory<br/>cache hit?"}
    Check1 -->|Yes| Return["Return MtlsBindingInfo"]
    Check1 -->|No| Check2

    Check2{"Windows cert<br/>store hit?"}
    Check2 -->|Yes| Promote["Promote to memory<br/>+ Return"] --> Return
    Check2 -->|No| Mint

    Mint["Mint new certificate<br/>1. GetOrCreateKey (KeyGuard RSA)<br/>2. Generate CSR<br/>3. POST /issuecredential<br/>4. Attach private key"]
    Mint --> Store["Store in both caches"]
    Store --> Return

    style Request fill:#4a90d9,color:#fff
    style Return fill:#27ae60,color:#fff
    style Mint fill:#e74c3c,color:#fff
```

---

## Error Handling & Fallback

```mermaid
flowchart TD
    Start["WithMtlsProofOfPossession() called"] --> P1

    P1{"Windows OS?"}
    P1 -->|No| E1["❌ MtlsNotSupportedForManagedIdentity<br/>MtlsNotSupportedForNonWindowsMessage"]
    P1 -->|Yes| P2

    P2{".NET Framework 4.6.2?"}
    P2 -->|Yes| E2["❌ MtlsNotSupportedForManagedIdentity<br/>MtlsNotSupportedForManagedIdentityMessage"]
    P2 -->|No| P3

    P3{"IMDSv2 available?"}
    P3 -->|No| E3["❌ MtlsPopTokenNotSupportedinImdsV1<br/>(IMDS returned 404 on /getplatformmetadata)"]
    P3 -->|Yes| P4

    P4{"KeyGuard key available?"}
    P4 -->|No| E4["❌ mtls_pop_requires_keyguard<br/>mTLS PoP requires KeyGuard keys"]
    P4 -->|Yes| P5

    P5{"mTLS connection succeeds?"}
    P5 -->|"SCHANNEL failure<br/>(bad cert)"| Retry["Remove bad cert from caches<br/>Mint fresh cert → Retry once"]
    Retry --> P5
    P5 -->|Success| Success["✅ mtls_pop token returned"]

    style E1 fill:#e74c3c,color:#fff
    style E2 fill:#e74c3c,color:#fff
    style E3 fill:#e74c3c,color:#fff
    style E4 fill:#e74c3c,color:#fff
    style Success fill:#27ae60,color:#fff
    style Retry fill:#f39c12,color:#fff
```
