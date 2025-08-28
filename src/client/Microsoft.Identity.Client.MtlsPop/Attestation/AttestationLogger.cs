// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.MtlsPop.Attestation
{
    internal static class AttestationLogger
    {
        /// <summary>Default logger that pipes native messages to <c>Console.WriteLine</c>.</summary>
        internal static readonly AttestationClientLib.LogFunc ConsoleLogger = (_,
            tag, lvl, func, line, msg) =>
            Console.WriteLine($"[{lvl}] {tag} {func}:{line}  {msg}");
    }
}
