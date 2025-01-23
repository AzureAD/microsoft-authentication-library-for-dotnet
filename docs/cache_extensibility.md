# MSAL Cache Key Extensibility Proposal

This document looks at defining a strategy for extending how tokens are cached internally by MSAL, allowing app developers to cache multiple tokens by a custom key.

## Functional Requirements

- Allow higher level SDKs to extend the default cache key semantics that all MSALs come with.
- Non-breaking changes
- Implementations MUST be consistent across confidential client MSALs
- `AcquireTokenForClient` (client_credentials) MUST support this mechanism. Other confidential client flows (web app, web api, ROPC) SHOULD support it.

## Non-Functional Requirements

- Performance of cache look-up operations does not degrade.
- Forward compatibility strategy - older MSALs must continue to work on the with the same cache as the new ones, to support upgrade scenarios. If this is not possible, an intermediate version of MSAL must be released that supports this.
- Logging must be extended so that MSAL developers can understand a cache miss.

### Example scenarios that will use this extensibility point

- Associate tokens with the client certificate used to obtain them.
- Associtate tokens with the client secrets used to obtain them.
- Allow an application to associate tokens with a SPIFEE type of identifier which is known upfront by the clients.

### Prior art

The cache key schema is defined [here](https://identitydivision.visualstudio.com/DevEx/_git/AuthLibrariesApiReview?path=/SSO/Schema.md) and extended for POP tokens [here](https://identitydivision.visualstudio.com/DevEx/_git/AuthLibrariesApiReview?path=/SSO/change_proposals/11232019-accesstoken_with_authscheme.md). A related cache extensiblity enhancement has been done [here](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4922).

## Developer Experience

#### MSAL 

```csharp

var app = ConfidentialClientApplicationBuilder.Create("client_id")             
              .WithClientCertifiacte(x509Cert)
              .WithExperimentalFeatures(true)               // All extensiblity APIs remain experimental
              .BuildConcrete();

var result = await app.AcquireTokenForClient("https://graph.microsoft.com/.default")
    .WithAdditionalCacheKeyComponents(new Dictionary<string, string>{ // New API
        {"cert_thubprint", x509Cert.GetSha2THumbprint() }, 
        {"spiffee_id", "37" }
    });
    .ExecuteAsync();   
```

After executing the AcquireToken instruction, MSAL shall associate the token with the existing components - authority, scope and client_id. In addition, it will assocaite the token with "cert_thumbprint" and "spifee_id". 

#### Higher level APIs

Do not expose this logic in higher level APIs.

## Token Schema changes

### Cache Key

Similar to the [POP](https://identitydivision.visualstudio.com/DevEx/_git/AuthLibrariesApiReview?path=/SSO/change_proposals/11232019-accesstoken_with_authscheme.md&_a=preview) enhancement, a new credential type will be used - `atext`.

The access token cache key will also add a suffix composed as follows (all operations are ordinal case sensitive):

1. Take the key-value pair list of components and **order** it alphabetically by the key (e.g. "key1": "val1", "key2": "val2")
1. Concatenate this list  (e.g. "key1val1key2val2")
1. Hash this using SHA256 - (e.g. `cc252f65706f969930208e4b2403435a95f7f7d9c964bd190ae2c6e032938235`)

So for the token in the example above the cache entry will be: 

`-login.microsoftonline.com-atext-client_id-tenant_id-https://graph.microsoft.com/.deafult-cc252f65706f969930208e4b2403435a95f7f7d9c964bd190ae2c6e032938235`

### Cache payload

Each key value pair must be included in the cache payload. Avoid collisions with existing documented keys.

Example: 

```json
{
  "AccessToken": {
    "-login.microsoftonline.com-atext-client_id-tenant_id-https://graph.microsoft.com/.deafult-cc252f65706f969930208e4b2403435a95f7f7d9c964bd190ae2c6e032938235": {
      "home_account_id": "6afc833f-49c0-4fd5-b685-2998a6cc8d8d.469fdeb4-d4fd-4fde-991e-308a78e4bea4",
      "environment": "login.microsoftonline.de",     
      "client_id": "0615b6ca-88d4-4884-8729-b178178f7c27",
      "secret": "eyJ..",
      "credential_type": "atext", // new!
      "realm": "469fdeb4-d4fd-4fde-991e-308a78e4bea4",
      "target": "https://graph.cloudapi.de/62e90394-69f5-4237-9190-012177145e10 https://graph.cloudapi.de/.default",      
      "cached_at": "1553819803",
      "expires_on": "1553823402",
      "key1": "val1",  // new!
      "key2": "val2",  // new! }
    }
}
```

Note: in case the values contain reserved JSON characters, use standard JSON escape rules to serialize.

### Cache lookup logic

Both the hash value and the actual payload values must be taken into account when performing cache lookups.

### External cache key

MSALs which suggest a distributed cache key must include this differentiator in the cache key

### Interop with POP key

Leave the keyID logic as is. We can come back to this.

### User token cache key

In user flows, only the access tokens will be cached by the new schema. Refresh tokens and Id tokens remain shared. For simplicity, do **not** extend this API to user flows until a few scenarios crop up!

## Acceptance tests

1. The following tests assume a call to `AcquireTokenForClient` with `WithAdditionalCacheKeyComponents` set to "key1"="val1", "key2"="val2". Always assert suggested cache key.

- Call `AcquireTokenForClient` with the same "key1"="val1", "key2"="val2". Assert cache hit.
- Call `AcquireTokenForClient` with no extensibility. Assert cache miss.
- Call `AcquireTokenForClient` with the same "key1"="val1". Assert cache miss.
- Call `AcquireTokenForClient` with the same "key1"="val1", "key2"="foo". Assert cache miss.
- Call `AcquireTokenForClient` with the same "Key1"="val1", "key2"="val2". Assert cache miss (capital "K" used in "key1")

2. Forwards compatibility test: old MSAL must function side by side with new MSAL, i.e. old MSAL must ignore the new access token cache entries. 

3. Try to use `WithAdditionalCacheKeyComponents` set to `client_id` -> error
4. POP Access token (all known schemes) and WithAdditionalCacheKeyComponents. Assert external cache key.
