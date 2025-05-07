# The Orchestrator – High‑Level Specification

## Purpose

*Provide a single service/ci pipeline (“The Orchestrator”)* that – for every internal micro‑service – centralises token acquisition (via ID Web + MSAL) and token validation / revocation (via token validation and cae modules).

At runtime the Orchestrator handles:

- Acquire a Continuous‑Access‑Evaluation‑ready token (cp1) using Managed Identity.

- Call custom Web APIs, passing the token.

- Validate inbound tokens through Validation SDKs + CAE modules.

- React to revocations by issuing claim‑challenges; ID Web/MSAL transparently refresh.

## Components & Relationships

| Layer                | Role                                                             | Technology                               |
|----------------------|------------------------------------------------------------------|------------------------------------------|
| **Token Acquisition**| Generates outbound tokens                                        | **Microsoft.Identity.Web** → **MSAL**    |
| **Token Validation** | Validates inbound tokens; triggers revocation via CAE            | **Validate SDK** + CAE module                    |
| **Lab Helper Service**| Control-plane utility that forcibly revokes tokens in test labs | Internal helper service                  |
| **Custom Web API**   | Resource server that trusts MISE validation                      | Runs behind the Orchestrator             |

## 4 CI / CD Integration

| Stage        | Action                                                                                                              |
|--------------|---------------------------------------------------------------------------------------------------------------------|
| **Build**    | **Orchestrator** builds **MSAL**, **ID Web**, **Validate SDK** and pusblishes a **Custom Web API**. |
| **Test (CI)** | Ensures token has xms_cc claims on **MSAL** and **ID Web**. |
| **Test (CI)** | Uses the token and calls in to the DownStream API. |
| **Test (CI)** | Validate SDKs does header validations. |
| **Test (Lab)** | Lab Helper service forcibly revokes the token via CAE modules. |
| **Test (CI)** | Validate SDKs does header validations. |
| **Test (CI)** | Uses the token and calls in to the DownStream API and gets a 401. |
| **Test (CI)** | Gets a new token using claims on DownStream API. **Microsoft.Identity.Web** → **MSAL** |
| **Test (CI)** | Uses the token and calls in to the DownStream API. |
| **Deploy**   | Helm chart publishes Orchestrator and Web API behind the internal gateway with **mTLS** enforced.                   |


## End‑to‑End Token Lifecycle

```mermaid
sequenceDiagram
    autonumber
    participant CIP as Azure DevOps Pipeline
    participant Orchestrator
    participant IDWeb as ID Web / MSAL
    participant CustomAPI as Custom Web API
    participant ValSDK as Validate SDK + CAE
    participant Helper as Lab Helper

    CIP->>Orchestrator: CI job requests /orchestrate
    Orchestrator->>IDWeb: Get MSI token (scope = WebAPI)
    IDWeb->>MSAL: AcquireTokenForManagedIdentity (xms_cc=cp1)
    MSAL-->>IDWeb: JWT
    Orchestrator->>CustomAPI: Call, Authorization: Bearer <JWT>
    CustomAPI->>ValSDK: Validate token
    ValSDK-->>CustomAPI: 200 OK (token valid)
    CustomAPI-->>Orchestrator: Data
    Orchestrator-->>CIP: Data

    Note over Helper,ValSDK: Lab Helper revokes the JWT via CAE

    CIP->>Orchestrator: Same request with cached JWT
    Orchestrator->>CustomAPI: Call with old JWT
    CustomAPI->>ValSDK: Validate (token revoked)
    ValSDK-->>CustomAPI: 401 w/ claims (cp1 flow)
    CustomAPI-->>Orchestrator: 401 + claims
    Orchestrator->>IDWeb: Retry – pass claims
    IDWeb->>MSAL: Force cache‑bypass, get new JWT
    MSAL-->>IDWeb: new JWT
    Orchestrator->>CustomAPI: Re‑call with fresh JWT
    CustomAPI->>ValSDK: Validate (success)
    ValSDK-->>CustomAPI: 200 OK
    CustomAPI-->>Orchestrator: Data
    Orchestrator-->>CIP: Data (success after retry)
```