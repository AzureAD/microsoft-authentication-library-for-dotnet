// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2.Schema;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    /// <summary>
    /// This does most of the raw work of IStorageManager but without knowledge of cross cutting concerns
    /// like telemetry.
    /// </summary>
    internal interface IStorageWorker
    {
        IEnumerable<Credential> ReadCredentials(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            string target,
            ISet<CredentialType> types);

        void WriteCredentials(IEnumerable<Credential> credentials);

        void DeleteCredentials(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            string target,
            ISet<CredentialType> types);

        Microsoft.Identity.Client.CacheV2.Schema.Account ReadAccount(string homeAccountId, string environment, string realm);
        void WriteAccount(Microsoft.Identity.Client.CacheV2.Schema.Account account);
        void DeleteAccount(string homeAccountId, string environment, string realm);
        void DeleteAccounts(string homeAccountId, string environment);
        AppMetadata ReadAppMetadata(string environment, string clientId);
        void WriteAppMetadata(AppMetadata appMetadata);
    }
}
