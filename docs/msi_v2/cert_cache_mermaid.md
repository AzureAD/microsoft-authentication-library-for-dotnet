# 1) Concurrency + rehydrate + mint (sequence view)

## The flow:

- Semaphore collapses concurrent mints for the same (tenant, mi, token_type).
- We re-check the cache after acquiring the gate to avoid double work.
- Store rehydrate is attempted; public‑only certs trigger a purge path.
- Mint happens only on true misses; final step posts to the mTLS token endpoint.

```mermaid
sequenceDiagram
    autonumber
    participant App
    participant ImdsV2 as ImdsV2Source
    participant Mgr as MsiCertManager
    participant InMem as InMemoryCache
    participant Store as BindingStore
    participant IMDS as IMDSv2
    participant TokenEP as MtlsAuthTokenEP

    App->>ImdsV2: AcquireToken()
    ImdsV2->>IMDS: GET /metadata/identity/getplatformmetadata
    IMDS-->>ImdsV2: csrMetadata
    ImdsV2->>Mgr: GetOrMintBinding(key, tenant, mi, type)

    Mgr->>InMem: TryGetLatest(memKey)
    InMem-->>Mgr: miss
    Mgr->>Mgr: Acquire semaphore(memKey)

    alt subject match in memory
        Mgr->>InMem: TryGetLatestBySubject(CN/DC)
        InMem-->>Mgr: entry
        Mgr->>InMem: Put(memKey, entry)
    else rehydrate from store
        Mgr->>Store: TryResolveFreshestBySubjectAndType(CN, DC, type)
        Store-->>Mgr: cert + endpoint OR public-only cert
        opt public-only
            Mgr->>Store: TryRemoveByThumbprintIfUnusable()
        end
    end

    alt need mint
        ImdsV2->>IMDS: POST /metadata/identity/issuecredential (CSR + attestation if PoP)
        IMDS-->>ImdsV2: CertificateRequestResponse
        ImdsV2-->>Mgr: resp + privateKey
        Mgr->>InMem: Put(memKey, entry)
        Mgr->>Store: TryInstallWithFriendlyName()
    end

    Mgr-->>ImdsV2: cert + resp
    ImdsV2->>TokenEP: POST /oauth2/v2.0/token (mTLS)
    TokenEP-->>ImdsV2: access_token
    ImdsV2-->>App: AuthenticationResult
```

# 2) IMDSv2 token acquisition (end‑to‑end flow)

## The Flow
- We always probe IMDSv2, then branch on token_type (mtls_pop vs bearer).
- Binding retrieval is cache-first, then subject-level, then store rehydrate, and finally mint.
- If store rehydrate yields a public‑only cert (no key) we purge it and mint.
- On success we cache and best‑effort install with a compact FriendlyName.

```mermaid
flowchart TD

    A["AcquireToken(resource, token_type)"] --> B["GET /metadata/identity/getplatformmetadata"]
    B -->|IMDSv2 available| C{"token_type"}
    C -->|mtls_pop| C1["Normalize attestation endpoint"]
    C -->|bearer| D
    C1 --> D["GetOrMintBinding(identityKey, tenant, mi, token_type)"]

    D --> E{"In-memory cache hit?"}
    E -->|yes| E1["Cache binding & ensure store; return cert+resp"]
    E -->|no| F["Acquire per-key semaphore"]

    F --> G{"Re-check in-mem cache"}
    G -->|hit| E1
    G -->|miss| H{"Try in-memory by subject (CN/DC)"}

    H -->|found| H1["Re-index under (tenant,mi,token_type); return"]
    H -->|not found| I{"Try user store by subject + token type"}

    I -->|found & key usable| I1["Detach cert from store; cache; return"]
    I -->|found but key missing| I2["Purge stale store entry"]
    I2 --> J["Mint binding via /issuecredential"]
    I -->|not found| J

    J --> K["Attach private key; cache; best-effort install with FriendlyName"]
    K --> L["POST mtlsauth /oauth2/v2.0/token with mTLS cert"]
```
