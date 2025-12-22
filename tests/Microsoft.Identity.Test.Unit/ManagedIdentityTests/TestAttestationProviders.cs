// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Test attestation provider delegates for unit testing.
    /// </summary>
    internal static class TestAttestationProviders
    {
        /// <summary>
        /// Creates a fake attestation provider delegate that returns a mock JWT.
        /// </summary>
        public static Func<string, SafeHandle, string, CancellationToken, Task<string>> CreateFakeProvider()
        {
            return (attestationEndpoint, keyHandle, clientId, cancellationToken) =>
            {
                return Task.FromResult("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.fake.attestation.sig");
            };
        }

        /// <summary>
        /// Creates an attestation provider delegate that returns null (for non-attested flow testing).
        /// </summary>
        public static Func<string, SafeHandle, string, CancellationToken, Task<string>> CreateNullProvider()
        {
            return (attestationEndpoint, keyHandle, clientId, cancellationToken) =>
            {
                return Task.FromResult<string>(null);
            };
        }

        /// <summary>
        /// Creates an attestation provider delegate that returns empty/whitespace token (for error testing).
        /// </summary>
        public static Func<string, SafeHandle, string, CancellationToken, Task<string>> CreateEmptyProvider()
        {
            return (attestationEndpoint, keyHandle, clientId, cancellationToken) =>
            {
                return Task.FromResult("   ");
            };
        }

        /// <summary>
        /// Creates an attestation provider delegate that throws an exception.
        /// </summary>
        public static Func<string, SafeHandle, string, CancellationToken, Task<string>> CreateFailingProvider(string errorMessage = "Attestation failed")
        {
            return (attestationEndpoint, keyHandle, clientId, cancellationToken) =>
            {
                throw new InvalidOperationException(errorMessage);
            };
        }

        /// <summary>
        /// Creates a counting attestation provider that tracks how many times it's called.
        /// </summary>
        public static CountingAttestationProvider CreateCountingProvider()
        {
            return new CountingAttestationProvider();
        }

        /// <summary>
        /// Attestation provider delegate wrapper that counts calls.
        /// </summary>
        public class CountingAttestationProvider
        {
            private int _callCount;

            public int CallCount => _callCount;

            public Func<string, SafeHandle, string, CancellationToken, Task<string>> GetDelegate()
            {
                return async (attestationEndpoint, keyHandle, clientId, cancellationToken) =>
                {
                    Interlocked.Increment(ref _callCount);
                    await Task.Yield();
                    return "header.payload.sig";
                };
            }
        }
    }
}
