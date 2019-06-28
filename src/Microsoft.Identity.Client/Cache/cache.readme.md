# Notes on the Unified Cache

# Notes on the Unified Cache

## Goals of Unified Cache

Details in the [wiki](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/changes-adalnet-4.0-preview).

### B2C notes

ADAL does not support B2C and there are no plans to add B2C support. As such, MSAL will not write B2C tokens to the ADAL token cache.
In B2C, currently, the displayName (aka username aka preferred username) is null. This is a "bug" in B2C as they should provide a scope for the username. DisplayName should never be null - it would be a schema violation for it to be null. We have code that adds a constant (smth like "Missing from the token response") in these cases.

### Account removal algorithm

When removing an account, a removal is performed on both the MSAL and the ADAL cache

##### RemoveAdalUser

If the accountId is not null (note that the account id comes from ClientInfo), then delete everything with the same 
clientInfo and env. 
Otherwise, delete everything with the same diplayable id (aka preferred username) and env.

Note:
- RemoveAdalUser is not scoped on ClientId, i.e. it will delete accoutns from different ClientIDs that match the criteria above
- GetAccounts and RemoveAccount do not work for ClientCredentail Grant

### Cache Schema

The location, keys and values of the items stored in the cache are standardized to allow interop (Xamarin iOS <-> obj-c iOS; Xamarin Android <-> Android; interal C++ <-> other platforms). 
The document is here:
https://identitydivision.visualstudio.com/DevEx/DevEx%20Team/_git/AuthLibrariesApiReview/pullrequest/540?path=%2FUnifiedSchema%2FSchema.md&_a=files
(we can make this document public on request, keeping it on VSTS just for ease of sharing internally)

Key calculation is platform specific because there are specificities on iOS (multiple keys) and UWP (key size limit of 255 chars).

To ensure key uniqueness, scopes must be normalized (lowercased and oredred alphabetically).

## Other notes

MSAL is able able to retrieve objects of type IAccount from the MSAL cache. 
The token cache is scoped on Env and ClientId.
When upgrading from ADALv3, the ClinetInfo is null because we do not request an IdToken;
ClientCredetialGrant - there is also no IdToken -> no ClientInfo. However, this uses a different token cache (AppTokenCache instead of UserTokenCache). This token cache is not unified, i.e. AT obtained from MSAL will not be written back to ADAL (need to verify this statement).


