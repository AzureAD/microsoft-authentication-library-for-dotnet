
# Proposal: Migrating HTTP Client Logic for FMI Credential from ServiceFabricFmiClientAssertionProvider's to MSAL's Managed Identity

## Overview
This proposal outlines the rationale and design for moving the HTTP client logic used to acquire an FMI credential currently implemented in MISE to the MSAL Managed Identity application. The credential is retrieved from an endpoint specified by the `APP_IDENTITY_ENDPOINT` and is used as a client credential for another MSAL application. This credential is an implementation of IdWebs `ClientAssertionProviderBase`. This logic can be seen in ServiceFabricFmiClientAssertionProvider.cs. The credential logic from this file will be moved to MSAL so that MSAL can acquire the FMI credential using its existing service fabric managed identity source and handle issues related to the http client. A new managed identity source can be added to the application that is based on the already existing service fabric managed identity source since the logic is very similar. 

## Motivation
- **Low-Level HTTP Handling**: The existing logic is implemented at a low level, which is inconsistent with the abstraction provided by MSAL and its service fabric integration.
- **MSAL Enhancements**: MSAL includes numerous improvements and bug fixes for Managed Identity and service fabric communication, particularly in HTTP client handling and retry logic.
- **Complex Integration**: Exposing the updated HTTP client logic to MISE would require significant additional logic in both MSAL and MISE, leading to increased complexity and maintenance overhead.
- **Code Consolidation**: Centralizing the logic within MSAL ensures a cleaner, more maintainable architecture and aligns with the principle of single responsibility.

## Proposed Design
MSAL will provide the FMI credential through MITS from the RMA node by crafting a request based on the FMI env variables on the machine. The credential and its expiration, 2 things needed by MISE to craft the client assertion, will be in the authentication result provided by MSAL.
To support this migration, I propose two approaches to trigger this flow in MSAL:

### 1. Explicit API
Introduce a dedicated API in MSAL to enable this flow explicitly, such as `WithServiceFabricFmi()`. This makes the integration clear and controlled. This will be chained off of the AcquireTokenParameterBuilder for MI apps.

```csharp
    var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                                .WithExperimentalFeatures()
                                .WithServiceFabricFmi()
                                .Build();

    var result = await managedIdentityApp.AcquireTokenForManagedIdentity(someResource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
```


### 2. Implicit Trigger via Resource Detection
Automatically trigger the logic when a specific FMI resource is detected. This approach minimizes the need for explicit configuration and supports backward compatibility.

```csharp
    var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                                .WithExperimentalFeatures()
                                .Build();

    var result = await managedIdentityApp.AcquireTokenForManagedIdentity("api://AzureFMITokenExchange/.default") //Or the GUID version
                    .ExecuteAsync()
                    .ConfigureAwait(false);
```
#### NOTE Based on our discussion, we decided to go with approach 2. This flow will be triggered by passing in "api://AzureFMITokenExchange/.default", which is an FMI specific resource.
## Benefits
- **Improved Maintainability**: Reduces duplication and isolates credential acquisition logic within MSAL.
- **Enhanced Reliability**: Leverages MSAL’s robust HTTP handling and retry mechanisms.
- **Cleaner Integration**: Avoids polluting MISE with service fabric-specific logic.
- **Future-Proofing**: Positions the system for easier upgrades and enhancements as MSAL evolves.

## Other Considerations

- The tokens should not be cached? Caching will be handled in the other MSAL application that uses this token. MISE will expect to get a new token each time this flow is used.
- To reduce the footprint of this logic, the managed identity application used to create this can use a system assigned identity.
- Should this logic stay behind the experimental features flag as it will only be used by MISE and is not intended for public use.
