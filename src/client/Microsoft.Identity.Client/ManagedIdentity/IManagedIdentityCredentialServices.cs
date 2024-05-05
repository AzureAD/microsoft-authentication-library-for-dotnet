// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal interface IManagedIdentityCredentialService
    {
        Task<CredentialResponse> GetCredentialAsync();
    }
}
