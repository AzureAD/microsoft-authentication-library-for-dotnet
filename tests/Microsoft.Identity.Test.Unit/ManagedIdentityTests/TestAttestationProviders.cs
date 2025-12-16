// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Test attestation providers for unit testing.
    /// </summary>
    internal static class TestAttestationProviders
    {
        /// <summary>
        /// Fake attestation provider that returns a mock JWT.
        /// </summary>
        public static IAttestationProvider CreateFakeProvider()
        {
            return new FakeAttestationProvider();
        }

        /// <summary>
        /// Attestation provider that returns null (for error testing).
        /// </summary>
        public static IAttestationProvider CreateNullProvider()
        {
            return new NullAttestationProvider();
        }

        /// <summary>
        /// Attestation provider that returns empty/whitespace token (for error testing).
        /// </summary>
        public static IAttestationProvider CreateEmptyProvider()
        {
            return new EmptyAttestationProvider();
        }

        /// <summary>
        /// Attestation provider that counts calls.
        /// </summary>
        public static CountingAttestationProvider CreateCountingProvider()
        {
            return new CountingAttestationProvider();
        }

        private class FakeAttestationProvider : IAttestationProvider
        {
            public Task<AttestationResult> AttestKeyGuardAsync(
                string attestationEndpoint,
                SafeHandle keyHandle,
                string clientId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new AttestationResult
                {
                    Status = AttestationStatus.Success,
                    Jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.fake.attestation.sig",
                    ErrorMessage = null,
                    NativeErrorCode = 0
                });
            }
        }

        private class NullAttestationProvider : IAttestationProvider
        {
            public Task<AttestationResult> AttestKeyGuardAsync(
                string attestationEndpoint,
                SafeHandle keyHandle,
                string clientId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult<AttestationResult>(null);
            }
        }

        private class EmptyAttestationProvider : IAttestationProvider
        {
            public Task<AttestationResult> AttestKeyGuardAsync(
                string attestationEndpoint,
                SafeHandle keyHandle,
                string clientId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new AttestationResult
                {
                    Status = AttestationStatus.Success,
                    Jwt = "   ",
                    ErrorMessage = null,
                    NativeErrorCode = 0
                });
            }
        }

        public class CountingAttestationProvider : IAttestationProvider
        {
            private int _callCount;

            public int CallCount => _callCount;

            public Task<AttestationResult> AttestKeyGuardAsync(
                string attestationEndpoint,
                SafeHandle keyHandle,
                string clientId,
                CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _callCount);
                return Task.FromResult(new AttestationResult
                {
                    Status = AttestationStatus.Success,
                    Jwt = "header.payload.sig",
                    ErrorMessage = null,
                    NativeErrorCode = 0
                });
            }
        }
    }
}
