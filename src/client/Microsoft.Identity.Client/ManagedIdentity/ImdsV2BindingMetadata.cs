// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ManagedIdentity.V2;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Imds V2 binding metadata cached per certificate subject.
    /// </summary>
    internal class ImdsV2BindingMetadata
    {
        public CertificateRequestResponse Response { get; set; }
        public string CertificateSubject { get; set; }  // e.g., "CN=msal-imdsv2-binding-<id>"
    }
}
