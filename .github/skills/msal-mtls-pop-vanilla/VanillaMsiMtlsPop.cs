// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Client.Helpers
{
    /// <summary>
    /// Complete example implementation of MSI-based vanilla mTLS PoP flow.
    /// Demonstrates end-to-end pattern for acquiring tokens and calling resources.
    /// </summary>
    public class VanillaMsiMtlsPop : IDisposable
    {
        private readonly IManagedIdentityApplication _app;
        private readonly ResourceCaller _resourceCaller;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance with system-assigned managed identity.
        /// </summary>
        public VanillaMsiMtlsPop()
        {
            _app = ManagedIdentityApplicationBuilder.Create()
                .Build();
            _resourceCaller = new ResourceCaller();
        }

        /// <summary>
        /// Initializes a new instance with user-assigned managed identity (by client ID).
        /// </summary>
        /// <param name="userAssignedClientId">The client ID of the user-assigned managed identity.</param>
        public VanillaMsiMtlsPop(string userAssignedClientId)
        {
            ArgumentNullException.ThrowIfNull(userAssignedClientId);

            _app = ManagedIdentityApplicationBuilder.Create()
                .WithUserAssignedManagedIdentity(userAssignedClientId)
                .Build();
            _resourceCaller = new ResourceCaller();
        }

        /// <summary>
        /// Initializes a new instance with user-assigned managed identity (by resource ID).
        /// </summary>
        /// <param name="resourceId">The Azure resource ID of the user-assigned managed identity.</param>
        /// <param name="useResourceId">Pass true to indicate resource ID (vs. client ID).</param>
        public VanillaMsiMtlsPop(string resourceId, bool useResourceId)
        {
            ArgumentNullException.ThrowIfNull(resourceId);

            if (!useResourceId)
            {
                throw new ArgumentException(
                    "Use the constructor with (string userAssignedClientId) for client ID.", 
                    nameof(useResourceId));
            }

            _app = ManagedIdentityApplicationBuilder.Create()
                .WithUserAssignedManagedIdentity(resourceId: resourceId)
                .Build();
            _resourceCaller = new ResourceCaller();
        }

        /// <summary>
        /// Acquires an mTLS PoP token for the specified scope.
        /// </summary>
        /// <param name="scope">The target scope (e.g., "https://graph.microsoft.com/.default").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authentication result with mTLS PoP token and binding certificate.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            string scope,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(scope);

            var result = await _app
                .AcquireTokenForManagedIdentity(scope)
                .WithMtlsProofOfPossession()
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            // Validate result
            if (result.TokenType != Constants.MtlsPoPTokenType)
            {
                throw new InvalidOperationException(
                    $"Expected token type '{Constants.MtlsPoPTokenType}' but got '{result.TokenType}'.");
            }

            if (result.BindingCertificate == null)
            {
                throw new InvalidOperationException(
                    "BindingCertificate is null. This should not happen with mTLS PoP tokens.");
            }

            return result;
        }

        /// <summary>
        /// Acquires an mTLS PoP token and calls a resource, returning the response content.
        /// </summary>
        /// <param name="scope">The target scope (e.g., "https://graph.microsoft.com/.default").</param>
        /// <param name="resourceUrl">The URL of the resource to call.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response content as a string.</returns>
        public async Task<string> GetResourceDataAsync(
            string scope,
            string resourceUrl,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(scope);
            ArgumentNullException.ThrowIfNull(resourceUrl);

            // Acquire token
            var authResult = await AcquireTokenAsync(scope, cancellationToken)
                .ConfigureAwait(false);

            // Call resource
            var content = await _resourceCaller.CallResourceAndGetContentAsync(
                authResult,
                resourceUrl,
                cancellationToken)
                .ConfigureAwait(false);

            return content;
        }

        /// <summary>
        /// Disposes the managed identity application and resource caller.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _resourceCaller?.Dispose();
            // Note: IManagedIdentityApplication doesn't implement IDisposable in MSAL.NET 4.x
            _disposed = true;
        }
    }
}
