// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.Internal.Pop
{
    /// <summary>
    /// The plug‑in registers its attestor once at module load.
    /// otherwise Core keeps using the no‑op default.
    /// </summary>
    internal static class PopKeyAttestorProvider
    {
        private static IPopKeyAttestor _current = new NoOpPopKeyAttestor();
        internal static IPopKeyAttestor Current => _current;

        internal static void Register(Func<IPopKeyAttestor> factory)
        {
            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            Interlocked.CompareExchange(ref _current, factory(), _current);
        }
    }
}
