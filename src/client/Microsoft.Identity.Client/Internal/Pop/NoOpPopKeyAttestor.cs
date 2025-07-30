// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using System;

namespace Microsoft.Identity.Client.Internal.Pop
{
    /// <summary>
    /// Stub used when the plug‑in is not referenced.
    /// </summary>
    internal class NoOpPopKeyAttestor : IPopKeyAttestor
    {
        public Task<byte[]> AttestAsync(
            SafeNCryptKeyHandle _,
            string __,
            string ___,
            CancellationToken ____)
            => Task.FromResult(Array.Empty<byte>());
    }

}
