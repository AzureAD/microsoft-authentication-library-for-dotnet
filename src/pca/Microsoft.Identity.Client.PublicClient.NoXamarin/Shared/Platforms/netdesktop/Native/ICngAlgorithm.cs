//--------// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if DESKTOP
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Platforms.net45.Native
{
    interface ICngAlgorithm
    {
        /// <summary>
        ///     Gets the algorithm or key storage provider being used for the implementation of the CNG
        ///     algorithm.
        /// </summary>
        CngProvider Provider { get; }
    }
}
#endif
