# MSAL Support for MSI v1 Token Revocation and Capabilities

---

## Goals

1. App developers and higher level SDKs like Azure SDK rely on the [CAE protocol](https://learn.microsoft.com/en-us/entra/identity-platform/app-resilience-continuous-access-evaluation?tabs=dotnet) for token revocation scenarios (`WithClaims`, `WithClientCapabilities`). 
1. RPs are enabled to perform token revocation.
1. A telemetry signal for eSTS exists to differentiate which apps are CAE enlightened.

## Flow diagram - revocation event

```mermaid
sequenceDiagram
    participant Resource
    actor CX as Client/Caller
    participant MSAL as MSAL on Leaf
    participant MITS as MITS (Proxy)
    participant SFRP as SFRP (RP)
    participant eSTS

rect rgb(173, 216, 230)
    CX->>Resource: 1. Call resource with "bad" token T
    Resource->>CX: 2. HTTP 401 + claims C
    CX->>CX: 3. Parse response, extract claims C
    CX->>MSAL: 4. MSI.AcquireToken(...).WithClaims(C).WithClientCapabilities("cp1")
end

rect rgb(215, 234, 132)
    MSAL->>MSAL: 5. Looks up old token T in local cache
    MSAL->>MITS: 6. MITS_endpoint?xms_cc=cp1&token_sha256_to_refresh=SHA256(T)
    MITS->>SFRP: 7. (Forward request w/ cc=cp1, hash=SHA256(T))
    SFRP->>SFRP: 8. Another MSAL call AcquireTokenForClient(...).WithClientCapabilities(cp1).WithAccessTokenSha256ToRefresh(hash)
    SFRP->>eSTS: 9. eSTS issues a new token
end
```

Steps 1-4 fall to the Client (i.e. application using MSI directly or higher level SDK like Azure KeyVault). This is the **standard CAE flow**.
Steps 5-9 are new and show how the RP propagates the revocation signal.

### Explanation:
1. CX creates an **MSAL Client** with `.WithClientCapabilities(cp1)`, to let the token issuer know that it is capable of handling token revocations.
2. The client (CX) calls some **Resource** with token **T**.
3. The resource detects **T** is bad (revoked) and returns **401** + **claims C**.
4. CX parses the WWW-Authenticate header, extracts the claims **C** and uses MSAL **AcquireToken** with `.WithClaims(C)`.
5. MSAL inspects its cache first. If it finds a token, the token is considered to have been revoked. MSAL needs to tell the token issuer about it, so that the token issuer can also bypass its own cache.
6. MSAL calls **MITS** with `xms_cc=cp1&token_sha256_to_refresh=SHA256(T)`.
7. **MITS** uses the information to bypass its own caches and to get a new token from its upstream **SFRP**.
8. **SFRP** uses MSAL again to get a **new** token from eSTS.

> [!IMPORTANT]
> This design is only applicable to MIRP api-version=2025-03-30 (for App Service). api-version for service fabric remains unchanged. 

> [!NOTE]
> The SHA256 conversion is done by doing a Hex-encoded SHA-256 hash of the token (UTF-8). For example: BitConverter.ToString(SHA256(Encoding.UTF8.GetBytes(accessToken))). 
> Example - "test_token" -> "cc0af97287543b65da2c7e1476426021826cab166f1e063ed012b855ff819656"


> [!NOTE]  
>  ClientCapabilities is an array of capabilities. In case the app developer sends multiple capabilities, these will be sent to the RP as `MITS_endpoint?xms_cc=cp1,cp2,cp3`. The RP MUST pass "cp1" (i.e. the CAE capabilitiy) if it is included.

> [!NOTE]  
> Parameter / APIs names are not final.


## Flow diagram - non-revocation event

The client "enlightment" status is still propagated via the client capability "cp1".

```mermaid
sequenceDiagram
    actor CX
    participant MSAL
    participant MITS
    participant SFRP        
    participant eSTS

rect rgb(173, 216, 230)   
    CX->>MSAL: 1. MSI.AcquireToken <br/> WithClientCapabilities("cp1")
    MSAL->>MSAL: 2. Find and return token T in cache. <br/>If not found, goto next step.
end
rect rgb(215, 234, 132)    
    MSAL->>MITS: 3. Call MITS_endpoint?xms_cc=cp1
    MITS->>SFRP: 4. Forward request to SFRP
    alt Cache Hit
        SFRP->>MSAL: 5a. Return cached token
    else Cache Miss
        SFRP->>eSTS: 5b. Call CCA.AcquireTokenForClient SN/I cert <br/> WithClientCapabilities(cp1)
        eSTS->>SFRP: 6. Return new token
        SFRP->>MSAL: 7. Return token to MSAL
    end
end
```

### New MSAL API - WithAccessTokenToRefresh()

To support the RP, MSAL will add a new API for `ConfidentialClientApplication.AcquireTokenForClient` -  `.WithAccessTokenToRefresh(string tokenHash)`. This may be extended to other flows too in the future.

This API will be in a namespace that indicates it is supposed to be used by RPs - `Microsoft.Identity.Client.RP`.

#### Behavior

- MSAL will look in the cache first, for a non-expired token. If it exists:
  - If it matches the "Bad" token SHA256 thumbprint, then MSAL will log this event, ignore the token, and get another token from the STS
  - If it doesn't match, it means that a new token was already updated. Return it.
- If it doesn't exist, call eSTS

## `xms_cc` as a List Value (URL Encoding)

### **Multiple Capabilities**
The `xms_cc` parameter can hold **multiple** client capabilities, formatted as:  
`xms_cc=cp1,cp2,cp3`

#### **Processing on SFRP:**
1. **On the calling side** (MSAL → MITS → SFRP), always **URL-encode** `xms_cc`, because commas (`,`) must be encoded in queries.
2. **SFRP** must **URL-decode** and **split** on commas:
   ```csharp
   // Example: “cp1,cp2,cp3”
   string raw = HttpUtility.UrlDecode(request.Query["xms_cc"]);
   string[] caps = raw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
   var clientCapabilities = caps.Select(c => c.Trim());
   ```
3. **MITS** typically just passes `xms_cc` along to SFRP if it’s acting as a simple proxy.

> [!NOTE]  
> RPs or MITS should not bypass cache if a bad token is not passed by MSAL. 

#### Motivation

The *internal protocol* between the client and the RP (i.e. calling the MITS endpoint in case of Service Fabric), is a simplified version of CAE. This is because CAE is claims driven and involves JSON operations such as JSON doc merges. The RP doesn't need the actual claims to perform revocation, it just needs a signal to bypass the cache. As such, it was decided to not use the full claims value internally.

## End to End testing

Given the complexity of the scenario, it may not be easy to automate this. Here is the [guideline](https://microsoft.sharepoint.com/:w:/t/AzureMSI/ESBeuafJLZdNlSxkBKvjcswBD4FGVz0o6YJcf4mfDRSH-Q?e=2hJRUt).

## Reference

[Token Revocation docs](https://microsoft.sharepoint.com/:w:/t/AzureMSI/ETSZ_FUzbcxMrcupnuPC8r4BV0dFQrONe1NdjATd3IceLA?e=n72v65)
