# Short-Lived Credential (SLC) Revocation Specification

## Overview

This document outlines the design and implementation details for short-lived credential (SLC) revocation in MSI V2 scenarios. 

In the MSI v2 authentication flow, MSAL first obtains a credential from IMDS and uses it to request a token from eSTS. In some cases, eSTS may respond with an error code indicating that the credential is no longer valid. When this occurs, MSAL must pass the error code back to IMDS to request a new credential before retrying the token request with eSTS. 

## SLC Revocation Scenarios

- **Revoked Token Scenario:** The token issued by an SLC has been revoked. In this case, Entra ID considers the SLC as invalid.

- **Unspecified Credential Issue Scenario:** When eSTS returns an invalid_client error without a suberror code, MSAL treats it as an unspecified credential issue.

- **Claims Challenge Scenario:** An API rejects an access token due to insufficient claims, requiring MSAL to retrieve a new token with the necessary claims.

## **eSTS Response to Indicate Specific Error**

| Scenario                         | eSTS/Resource Response                                                                                 |
|----------------------------------|--------------------------------------------------------------------------------------------------------|
| Revoked Token                    | `{ "error": "invalid_client", "suberror": "revoked_token" }`                                           |
| Unspecified Credential Issue     | `{ "error": "invalid_client" }`                                                                        |

## **Resource Token Rejection Scenario**

A resource may reject a token due to various reasons, such as token expiration, or security policy violations. When this occurs:

- The resource will respond with `insufficient_claims`.
- MSAL will detect the resource rejection based on the claims API.
- MSAL will call IMDS with `error_code=revoked_token` to obtain a fresh credential before making a new token request to eSTS.

## **MSAL Behavior to Relay the Signal to IMDS**

- MSAL can only determine that an `{ "error": "invalid_client" }` response is caused by a credential issue but cannot handle suberrors explicitly.
- For SLC-related errors, MSAL will retry obtaining a new SLC from IMDS and retry with eSTS.
- For claims challenges, MSAL does not get a signal from eSTS but rather from the app developer when passing claims to MSAL.
- MSAL will **relay the response to IMDS as-is**, ensuring support for future suberror codes without requiring modifications.

### **MSAL Pseudo Code Implementation**

```csharp
var tokenResponse = HttpClient.post("https://ests-r/token", clientCredential=currentSLC);

if (tokenResponse.get("error") == "invalid_client") {
    if (tokenResponse.get("suberror") != empty) {
        suberror = tokenResponse.get("suberror");
    } else {
        suberror = "unspecified";
    }
    currentSLC = HttpClient.post(
        "http://169.254.169.254/.../credential?...&error_code=" + suberror
    );
    
    tokenResponse = HttpClient.post("https://ests-r/token", clientCredential=currentSLC);
}

return tokenResponse;
```

