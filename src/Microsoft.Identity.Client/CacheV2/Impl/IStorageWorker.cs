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