// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.CacheV2.Schema;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    /// <summary>
    /// Interface providing mechanism to transform the unified schema types into their appropriate "path"
    /// or "key" for storage/retrieval.  For example, on Windows, this will be a relative file system path.
    /// But on iOS/macOS is will be a path to keychain storage.
    /// </summary>
    internal interface ICredentialPathManager
    {
        string GetCredentialPath(Credential credential);
        string ToSafeFilename(string data);

        string GetCredentialPath(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            CredentialType credentialType);

        string GetAppMetadataPath(string environment, string clientId);
        string GetAccountPath(Microsoft.Identity.Client.CacheV2.Schema.Account account);
        string GetAccountPath(string homeAccountId, string environment, string realm);
        string GetAppMetadataPath(AppMetadata appMetadata);
        string GetAccountsPath(string homeAccountId, string environment);
    }
}
