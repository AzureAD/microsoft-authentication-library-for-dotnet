// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ManagedIdentity.V2.Cache
{
    internal sealed partial class ImdsV2BindingCache
    {
        private sealed class ImdsV2BindingMetadata
        {
            public string Subject;                         // first-wins, stable
            public CertificateRequestResponse BearerResponse;
            public CertificateRequestResponse PopResponse;
        }
    }
}
