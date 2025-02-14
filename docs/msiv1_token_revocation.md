# MSAL Support for MSI v1 Token Revocation and Capabilities

This specification describes **two optional parameters** that MSAL can send to the **MSI v1 endpoint** (e.g., `http://169.254.169.254/metadata/identity/oauth2/token`) when acquiring tokens via **Managed Identity**. These parameters empower developers to control **token revocation** and specify **client capabilities** that might alter the token issuance behavior by Azure AD (ESTS).

---

## Overview

When MSAL requests a token from the MSI v1 endpoint for resources like `https://management.azure.com/`, it typically reuses valid, locally cached tokens. However, certain scenarios (for e.g. [claims challenge](https://learn.microsoft.com/en-us/entra/identity-platform/claims-challenge?tabs=dotnet)) require explicitly **ignoring** the cache or specifying **client capabilities** that Azure AD uses to issue a fresh token.

To address these scenarios, two **optional** query parameters can be included in the MSI v1 token request:

1. **`bypass_cache`**  
   - Forces a token refresh if set to `true`.
2. **`xms_cc`**  
   - Declares special client capabilities that Azure AD should consider (e.g., for advanced or “undo revocation” scenarios).

---

## 1. `bypass_cache`

### Purpose

Allows MSAL to to **get a brand-new token** from Azure AD. When `bypass_cache=true`, MSAL will ignore any valid, cached token and ensure the MSI v1 endpoint fetches a **new** token from Azure AD rather than returning a cached one.

### Behavior
- If set to `true`, MSI encoded will skip it's internal cache and issue a fresh token.
- If set to `false` or omitted, MSI will return a valid cached token.

### Use Cases
- **Token Revocation**: Ensures any previously revoked or invalidated token is not served from cache.

---

## 2. `xms_cc`

### Purpose

Enables the MSAL to pass **client capabilities** in the token acquisition request. These capabilities are often used to unlock or disable specific features in Azure AD, such as handling specialized revocation scenarios.

### Behavior
- The value is typically a comma-separated list of capability strings.
- MSAL sends `xms_cc` to the MSI v1 endpoint, provided MSAL app developers have set these capabilities in the application.

### Use Cases
- **“Undo token revocation”** or other advanced features: By setting the required capabilities (`cp1`, etc.), 

---

## Usage 

App developers can already specify these parameters using existing APIs in MSAL. For instance:

```cs
// Example usage in MSAL (already shipped, no new APIs added)
var mi = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .WithClientCapabilities(ClientCapabilities) // e.g. ["cp1", "cp2"]
    .Build();

var result = await mi.AcquireTokenForManagedIdentity(new[] { "https://management.azure.com/.default" })
    .WithClaims(Claims)
    .ExecuteAsync()
    .ConfigureAwait(false);

```

## End to End testing 

Given the complexity of the scenario, it may not be easy to automate this. Here is the [guideline](https://microsoft.sharepoint.com/:w:/t/AzureMSI/ESBeuafJLZdNlSxkBKvjcswBD4FGVz0o6YJcf4mfDRSH-Q?e=2hJRUt) to test this feature manually using a Virtual Machine. For further details, please contact Gladwin. 

## Reference

[Token Revocation docs](https://microsoft.sharepoint.com/:w:/t/AzureMSI/ETSZ_FUzbcxMrcupnuPC8r4BV0dFQrONe1NdjATd3IceLA?e=n72v65)
