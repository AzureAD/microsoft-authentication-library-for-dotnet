// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client.Credential
{
    internal interface ICredentialResponseCache
    {
        Task<HttpResponse> GetOrFetchCredentialAsync(
            IHttpManager httpManager,
            ManagedIdentityRequest request,
            string key,
            CancellationToken cancellationToken);

        void AddCredential(string key, HttpResponse response);
        void RemoveCredential(string key);
    }
}
