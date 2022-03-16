// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;

namespace Microsoft.Identity.Client.Platforms.net45.Native
{
    /// <summary>
    ///     Interface for asymmetric algorithms implemented over the CNG layer of Windows to provide CNG
    ///     implementation details through.
    /// </summary>
    interface ICngAsymmetricAlgorithm : ICngAlgorithm
    {
        /// <summary>
        ///     Get the CNG key being used by the asymmetric algorithm.
        /// </summary>
        /// <permission cref="SecurityPermission">
        ///     This method requires that the immediate caller have SecurityPermission/UnmanagedCode
        /// </permission>
        CngKey Key
        {
            [SecurityCritical]
            get;
        }
    }
}
