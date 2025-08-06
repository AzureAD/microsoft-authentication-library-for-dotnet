// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.Internal.Pop
{
    /// <summary>
    /// Singleton access point for the Managed-Identity key provider.
    /// A real implementation registers itself at module-load time; otherwise
    /// the no-op stub is used.
    /// </summary>
    internal static class ManagedIdentityKeyProvider
    {
        private static IManagedIdentityKeyProvider _current =
            new NoOpManagedIdentityKeyProvider();    // default stub

        internal static IManagedIdentityKeyProvider Current => _current;

        /// <summary>
        /// Registers the concrete provider.  The first call wins; subsequent
        /// calls are ignored to avoid accidental replacement.
        /// </summary>
        internal static void Register(Func<IManagedIdentityKeyProvider> factory)
        {
            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            Interlocked.CompareExchange(ref _current, factory(), _current);
        }
    }
}
