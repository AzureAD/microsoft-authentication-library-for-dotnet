// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    /// <summary>
    /// Enumerates various types of crypto keys used in the application.
    /// </summary>
    internal enum CryptoKeyType
    {
        None = 0,         // No specific crypto key type.
        KeyGuard = 1,     // KeyGuard-protected key.
        Machine = 2,      // Machine key.
        User = 3,         // User Key.
        Ephemeral = 4,    // Ephemeral (short-lived) key.
        InMemory = 5     // In-memory key.
    }
}
