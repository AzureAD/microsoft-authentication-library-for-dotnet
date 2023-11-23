// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    /// <summary>
    /// Enumerates various types of crypto keys used in the application.
    /// </summary>
    public enum CryptoKeyType
    {
        None,         // No specific crypto key type.
        KeyGuard,     // KeyGuard-protected key.
        Machine,      // Machine key.
        User,         // User Key.
        Ephemeral,    // Ephemeral (short-lived) key.
        InMemory      // In-memory key.
    }
}
