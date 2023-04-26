// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Class to define a system assigned managed identity.
    /// </summary>
    public class SystemAssignedManagedIdentity : IManagedIdentity
    {
        /// <summary>
        /// Returns a default instance of <see cref="SystemAssignedManagedIdentity"/>.
        /// </summary>
        /// <returns></returns>
        public static IManagedIdentity Default()
        {
            return new SystemAssignedManagedIdentity();
        }
    }
}
