// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2.Schema;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    /// <summary>
    /// Equivalence layer with MSAL C++ and other native platforms for handing read/write/query operations
    /// on the various credential and account types in the unified cache.
    /// Also provides (on msal.net) access to the Adal Legacy Cache Manager for ensuring legacy cache interop
    /// during storage access.
    /// </summary>
    internal interface IStorageManager
    {
        IAdalLegacyCacheManager AdalLegacyCacheManager { get; }

        //event EventHandler<TokenCacheNotificationArgs> BeforeAccess;
        //event EventHandler<TokenCacheNotificationArgs> AfterAccess;
        //event EventHandler<TokenCacheNotificationArgs> BeforeWrite;
        byte[] Serialize();
        void Deserialize(byte[] serializedBytes);

        ReadCredentialsResponse ReadCredentials(
            string correlationId,
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            string target,
            ISet<CredentialType> types);

        OperationStatus WriteCredentials(string correlationId, IEnumerable<Credential> credentials);

        OperationStatus DeleteCredentials(
            string correlationId,
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            string target,
            ISet<CredentialType> types);

        ReadAccountsResponse ReadAllAccounts(string correlationId);

        ReadAccountResponse ReadAccount(
            string correlationId,
            string homeAccountId,
            string environment,
            string realm);

        OperationStatus WriteAccount(string correlationId, Microsoft.Identity.Client.CacheV2.Schema.Account account);

        OperationStatus DeleteAccount(
            string correlationId,
            string homeAccountId,
            string environment,
            string realm);

        OperationStatus DeleteAccounts(string correlationId, string homeAccountId, string environment);
        AppMetadata ReadAppMetadata(string environment, string clientId);
        void WriteAppMetadata(AppMetadata appMetadata);
    }
}
