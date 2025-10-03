// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal sealed class AttestationTokenInput
    {
        public string ClientId { get; set; }

        public Uri AttestationEndpoint { get; set; }

        /// <summary>
        /// The key handle of the assymetric algorithm to be attested. Currently, only RSA CNG is supported,
        /// available on Windows only, i.e. RSACng.Key.Handle.
        /// The handle must remain valid for the duration of the attestation call.
        /// </summary>
        public SafeHandle KeyHandle { get; set; }
    }
}
