// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Test extension methods for managed identity scenarios.
    /// </summary>
    internal static class ManagedIdentityTestExtensions
    {
        /// <summary>
        /// Test-only extension method to inject a custom attestation provider delegate.
        /// This bypasses the KeyAttestation package's production extension method.
        /// </summary>
        public static AcquireTokenForManagedIdentityParameterBuilder WithAttestationProviderForTests(
            this AcquireTokenForManagedIdentityParameterBuilder builder,
            Func<string, SafeHandle, string, CancellationToken, Task<string>> attestationTokenProvider)
        {
            builder.CommonParameters.AttestationTokenProvider = attestationTokenProvider;
            return builder;
        }
    }
}
