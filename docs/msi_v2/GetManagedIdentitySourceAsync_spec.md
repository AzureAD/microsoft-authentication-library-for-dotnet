# GetManagedIdentitySourceAsync API Improvements

Azure SDK has asked MSAL to make some changes to the `GetManagedIdentitySourceAsync` API - they are the primary user of the API. They want the option to toggle between imds1 vs imds2. They also want MSAL to probe imds1, as they currently do as a failsafe when MSAL determines the source to be imds1. With this in mind, an error should be thrown when probes for both imds2 and imds1 return failures.

ManagedIdentityApplication.cs:

```csharp
public async Task<ManagedIdentitySource> GetManagedIdentitySourceAsync(bool: imds2 = true) {
    // ...

    return await ManagedIdentityClient.GetManagedIdentitySourceAsync(csrMetadataProbeRequestContext, isMtlsPopRequested: true, imds2).ConfigureAwait(false);

}

public static ManagedIdentitySource GetManagedIdentitySource() {
    return ManagedIdentityClient.GetManagedIdentitySourceNoImdsV2(imds2: false);
}
```

ManagedIdentityClient.cs:

```csharp
internal async Task<ManagedIdentitySource> GetManagedIdentitySourceAsync(
RequestContext requestContext,
bool isMtlsPopRequested,
bool imds2
) {
    // ...

    ManagedIdentitySource source = GetManagedIdentitySourceNoImdsV2(requestContext.Logger, imds2);

    // probe Imds2
    // throw if not 400 response
}
```

```csharp
GetManagedIdentitySourceNoImdsV2(ILoggerAdapter logger = null, bool imds2) {
    // check each MSI source based on env variable

    // if normally would return DefaultToImds

        // if imds2 true, return returnDefaultToImds, so imds2 probe can occur in calling function (GetManagedIdentitySourceAsync)

        // if imds2 false, probe imds1
        // throw if not 400 response
}
```

ImdsManagedIdentitySource.cs:

ImdsV2ManagedIdentitySource.cs:

```csharp
bool sendProbe() {
    // send network request without metadata header
    // imds1: /metadata/identity/oauth2/token
    // imds2: /metadata/identity/getplatformmetadata

    // if available will respond with 400
}
```
