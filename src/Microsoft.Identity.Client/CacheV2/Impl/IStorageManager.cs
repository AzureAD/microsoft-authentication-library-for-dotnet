// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

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