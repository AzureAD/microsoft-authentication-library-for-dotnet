# Token Revocation Hackathon: RevoGuard

## 1. Purpose & Overview

**Goal**: Provide a sandbox to **revoke Managed Identity (MSI) tokens** and validate how resources and applications behave when presented with revoked or otherwise invalidated tokens. This helps teams:

1. Understand MSI-based authentication.  
2. Validate token revocation mechanisms (where possible) and observe the impacts in real-time.  
3. Strengthen security and compliance by confirming that stale or compromised tokens cannot be used to access resources.

**Why MSI?**  
Managed Identities provide a seamless way for Azure services to access Azure resources (Key Vault, Storage, etc.) without managing credentials. However, organizations want to ensure that when a token is compromised or must be invalidated, there is a robust, testable approach to revoking or expiring the token quickly.

---

## 2. Scope

1. **Azure Resources**  
   - Use of Azure resources that rely on MSI for authentication (e.g., Azure VM, App Service, Azure Function, AKS pod identity).  
   - Demonstrate token acquisition using the Managed Identity endpoint (IMDS for VMs, or the MSI endpoint for App Services).

2. **Token Issuance & Revocation**  
   - Acquire an MSI token for a given resource.  
   - Attempt to forcibly revoke or invalidate that token (within the constraints of Azure AD and standard OAuth flows).  
   - Observe the behavior of the resource and measure how long it takes until the resource no longer accepts that token.

---

## 3. Key Components & Architecture

The hackathon environment will have the following major components:

1. **Azure Active Directory (Entra ID)**  
   - Issues tokens for the Managed Identity.  
   - Responsible for any revocation list or instant revocation APIs.  
   - Provides logs on sign-ins and token issuance.

2. **Managed Identity Host**  
   - An Azure resource that supports Managed Identity (for example, a VM with System-Assigned MSI or an App Service).  
   - Retrieves tokens from the local MSI/IMDS endpoint.  
   - Uses tokens to authenticate to other Azure services.

3. **Target Resource**  
   - The resource or service being accessed using the MSI token. Examples: Azure Key Vault, Azure Storage, or a custom API protected by Azure AD.  
   - Validates incoming tokens against Azure AD.  
   - Contains logic/policies to verify if a token is still valid or has been revoked.

4. **Revocation Mechanism**  
   - We will be using an in-built solution for this.  
   - For the hackathon, we simulate “revocation” by performing actions such as:  
     - **Revoking** tokens issued on a identity.  
   - Observe how quickly and consistently the target resource respects the revoked token.
   - 

5. **Monitoring & Logging**  
   - Azure Monitor / Log Analytics for capturing sign-in logs, token issuance events, and failed token validations.  
   - Instrumentation in the application or scripts to measure the time from “revocation action” to “token rejection.”

**Diagram**:
```mermaid
sequenceDiagram
    participant AAD as Azure AD (Entra ID)
    participant Host as Managed Identity Host
    participant Resource as Target Resource
    participant Revo as Revocation Mechanism
    participant Monitor as Monitoring & Logging

    rect rgb(245, 245, 245)
    Note over Host: 1) Host requests MSI token
    Host->>AAD: Request token (MSI/IMDS endpoint)
    AAD-->>Host: Returns short-lived access token
    Host->>Resource: Presents access token
    Resource-->>Host: Access granted
    end

    rect rgb(245, 245, 245)
    Note over Revo: 2) Revoke token via RevoGuard
    Revo->>AAD: Revocation action (invalidate token)
    AAD-->>Resource: Updated revocation status
    Host->>Resource: Attempts to use old token
    Resource-->>Host: Access denied (revoked token)
    end

    rect rgb(245, 245, 245)
    Note over Monitor: 3) Observe logs & metrics
    Monitor->>AAD: Collect sign-in & token logs
    Monitor->>Resource: Collect access & denial events
    end

    rect rgb(245, 245, 245)
    Note over Host: 4) Host acquires new token & retries
    Host->>AAD: Request new token (MSI/IMDS endpoint)
    AAD-->>Host: Returns fresh short-lived token
    Host->>Resource: Presents new valid token
    Resource-->>Host: Access granted (new token)
    end
```
