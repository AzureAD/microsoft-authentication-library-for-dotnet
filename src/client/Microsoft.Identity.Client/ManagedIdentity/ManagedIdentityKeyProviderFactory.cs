// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Identity.Client.ManagedIdentity.Providers;
using Microsoft.Identity.Client.PlatformsCommon.Shared; 

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal static class ManagedIdentityKeyProviderFactory
    {
        private static IManagedIdentityKeyProvider s_provider;

        internal static IManagedIdentityKeyProvider GetOrCreateProvider()
        {
            var p = Volatile.Read(ref s_provider);
            if (p != null)
                return p;

            IManagedIdentityKeyProvider created = CreateProviderCore();
            Interlocked.CompareExchange(ref s_provider, created, null);
            return s_provider!;
        }

        private static IManagedIdentityKeyProvider CreateProviderCore()
        {
#if NETSTANDARD2_0
            return new InMemoryManagedIdentityKeyProvider();
#else
            if (DesktopOsHelper.IsWindows())
            {
                return new WindowsManagedIdentityKeyProvider();
            }
            else
            {
                return new InMemoryManagedIdentityKeyProvider();
            }
#endif
        }
    }
}
