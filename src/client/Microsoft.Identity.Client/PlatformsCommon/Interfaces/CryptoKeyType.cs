// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    /// <summary>
    /// Defines an enumeration of different types of cryptographic keys that can be used within the application.
    /// Each member of this enum represents a specific type of cryptographic key with a unique purpose.
    /// </summary>
    internal enum CryptoKeyType
    {
        /// <summary>
        /// Represents an undefined cryptographic key type when MSAL is not able to identify the key type. 
        /// Used as a default value when no specific key type is applicable.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Represents a cryptographic machine key protected by KeyGuard. This key is typically used for operations 
        /// requiring higher security enforced by the system hardware (e.g., to acquire Proof-of-Possession tokens).
        /// </summary>
        KeyGuardMachine = 1,

        /// <summary>
        /// Represents a cryptographic user key protected by KeyGuard. This key is user-specific and provides 
        /// the same security measures like a machine key, but is only used to acquire Continuous Access Evaluation tokens.
        /// </summary>
        KeyGuardUser = 2,
    }
}
