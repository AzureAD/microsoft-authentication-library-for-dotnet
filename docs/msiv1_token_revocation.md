# MSAL Support for MSI v1 Token Revocation and Capabilities

This specification describes **two optional parameters** that MSAL can send to the **MSI v1 endpoint** (e.g., `http://169.254.169.254/metadata/identity/oauth2/token`) when acquiring tokens via **Managed Identity**. These parameters empower developers to control **token revocation** and specify **client capabilities** that might alter the token issuance behavior by Azure AD (ESTS).

---

## Overview

When MSAL requests a token from the MSI v1 endpoint for resources like `https://management.azure.com/`, it typically reuses valid, locally cached tokens. However, certain scenarios require explicitly **ignoring** the cache or specifying **client capabilities** that Azure AD uses to enable or disable particular features.

To address these scenarios, two **optional** query parameters can be included in the MSI v1 token request:

1. **`bypass_cache`**  
   - Forces a token refresh if set to `true`.
2. **`xms_cc`**  
   - Declares special client capabilities that Azure AD should consider (e.g., for advanced or “undo revocation” scenarios).

---

## 1. `bypass_cache`

### Purpose

Allows the developer to **get a brand-new token** from Azure AD. When `bypass_cache=true`, MSAL will ignore any valid, cached token and ensure the MSI v1 endpoint fetches a **new** token from Azure AD rather than returning a cached one.

### Behavior
- If set to `true`, MSAL will send `bypass_cache=true` to the MSI endpoint.
- If set to `false` or omitted, MSAL can return a cached token (if one exists and is still valid).

### Use Cases
- **Token Revocation**: Ensures any previously revoked or invalidated token is not served from cache.

---

## 2. `xms_cc`

### Purpose

Enables the developer to specify **client capabilities** in the token acquisition request. These capabilities are often used to unlock or disable specific features in Azure AD, such as handling specialized revocation scenarios.

### Behavior
- The value is typically a comma-separated list of capability strings.
- MSAL sends `xms_cc` to the MSI v1 endpoint, which then relays these capabilities to Azure AD as part of a JSON-encoded `claims` parameter.

### Use Cases
- **“Undo token revocation”** or other advanced features: By setting the required capabilities (`cp1`, etc.), MSAL can influence how Azure AD issues the token.

---
