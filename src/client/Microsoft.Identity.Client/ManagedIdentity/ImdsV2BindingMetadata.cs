// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Identity.Client.ManagedIdentity.V2;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// IMDSv2 binding metadata cached per identity (MSAL client id).
    /// Thumbprints are stored per token_type ("Bearer", "mtls_pop").
    /// </summary>
    internal class ImdsV2BindingMetadata
    {
        public CertificateRequestResponse Response { get; set; }
        public string Subject { get; set; }  // same for Bearer and PoP

        // token_type -> thumbprint (e.g., "Bearer", "mtls_pop")
        public ConcurrentDictionary<string, string> ThumbprintsByTokenType { get; }
            = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
