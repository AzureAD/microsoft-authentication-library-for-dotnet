// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Indicates whether credential material was resolved from a static (compile-time) source
    /// or from a runtime callback.
    /// </summary>
    internal enum CredentialSource
    {
        /// <summary>The credential was supplied directly (e.g., a certificate or secret passed at build time).</summary>
        Static,

        /// <summary>The credential was obtained by invoking a user-supplied delegate at request time.</summary>
        Callback
    }
}
