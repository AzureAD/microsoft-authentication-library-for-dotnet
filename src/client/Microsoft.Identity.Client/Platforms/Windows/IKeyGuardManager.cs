// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Platforms.Windows
{
    /// <summary>
    /// Platform / OS specific logic.  
    /// </summary>
    public interface IKeyGuardManager
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
        bool IsKeyGuardProtected(CngKey cngKey);

        /// <summary>
        /// Check if the private key of the given certificate is protected by KeyGuard
        /// </summary>
        bool IsKeyGuardProtected(X509Certificate2 certificate);

        /// <summary>
        /// Checks if virtualization-based security is enabled on the device, allowing the functionality of KeyGuard
        /// </summary>
        /// <returns>True if virtualization-based security is available</returns>
        bool IsVBSEnabled();
    }
}
