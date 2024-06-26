// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Platform / OS specific logic.  
    /// </summary>
    internal interface IKeyGuardProxy
    {
        /// <summary>
        /// Load a CngKey with the given key provider
        /// </summary>
        /// <param name="keyProvider"></param>
        /// <returns></returns>
        ECDsa LoadCngKeyWithProvider(string keyProvider);

        /// <summary>
        /// Check if the given Cng key is protected by KeyGuard
        /// </summary>
        bool IsKeyGuardProtectedKey(CngKey cngKey);
    }
}
