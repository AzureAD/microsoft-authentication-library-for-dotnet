# Notes on the Unified Cache

# Notes on the Unified Cache

## Goals of Unified Cache

Details in the [wiki](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/changes-adalnet-4.0-preview).

### B2C notes

ADAL does not support B2C and there are no plans to add B2C support. As such, MSAL will not write B2C tokens to the ADAL token cache.

In B2C, currently, the displayName (aka username aka preferred username) is null. This is a "bug" in B2C as they should provide a scope for the username. DisplayName should never be null - it would be a schema violation for it to be null. We have code that adds a constant (smth like "preferred_username not in id_token") in these cases.

### Remove account algorithm

When removing an account, a removal is performed on both the MSAL and the ADAL cache

##### RemoveAdalUser

If the accountId is not null (note that the account id comes from ClientInfo), then delete everything with the same 
clientInfo and env. 
Otherwise, delete everything with the same diplayable id (aka preffered username) and env.

Note:
- RemoveAdalUser is not scoped on ClientId, i.e. it will delete accoutns from different ClientIDs that match the criteria above
- GetAccounts and RemoveAccount do not work for ClientCredentail Grant




## Other notes

MSAL is able able to retrieve objects of type IAccount from the MSAL cache. 
The token cache is scoped on Env and ClientId.
When upgrading from ADALv3, the ClinetInfo is null because we do not request an IdToken;
ClientCredetialGrant - there is also no IdToken -> no ClientInfo. However, this uses a different token cache (AppTokenCache instead of UserTokenCache). This token cache is not unified, i.e. AT obtained from MSAL will not be written back to ADAL (need to verify this statement).


