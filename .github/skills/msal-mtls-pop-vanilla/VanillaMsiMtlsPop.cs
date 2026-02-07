// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Concrete implementation demonstrating vanilla (direct) mTLS PoP token acquisition
    /// using Managed Identity with all supported identity types.
    /// </summary>
    /// <remarks>
    /// This class showcases:
    /// - System-assigned Managed Identity (SAMI)
    /// - User-assigned Managed Identity (UAMI) by Client ID, Resource ID, and Object ID
    /// - Direct resource calling with mTLS PoP tokens
    /// </remarks>
    public sealed class VanillaMsiMtlsPop : IDisposable
    {
        private readonly IManagedIdentityApplication _app;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance for the specified Managed Identity configuration.
        /// </summary>
        /// <param name="managedIdentityId">The managed identity configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when managedIdentityId is null.</exception>
        public VanillaMsiMtlsPop(ManagedIdentityId managedIdentityId)
        {
            ArgumentNullException.ThrowIfNull(managedIdentityId);

            _app = ManagedIdentityApplicationBuilder
                .Create(managedIdentityId)
                .Build();
        }

        /// <summary>
        /// Creates an instance for system-assigned Managed Identity (SAMI).
        /// </summary>
        /// <returns>A new VanillaMsiMtlsPop instance.</returns>
        public static VanillaMsiMtlsPop CreateForSystemAssigned()
        {
            return new VanillaMsiMtlsPop(ManagedIdentityId.SystemAssigned);
        }

        /// <summary>
        /// Creates an instance for user-assigned Managed Identity using Client ID.
        /// </summary>
        /// <param name="clientId">The client ID of the user-assigned identity (e.g., "6325cd32-9911-41f3-819c-416cdf9104e7").</param>
        /// <returns>A new VanillaMsiMtlsPop instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when clientId is null.</exception>
        public static VanillaMsiMtlsPop CreateForUserAssignedByClientId(string clientId)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            return new VanillaMsiMtlsPop(ManagedIdentityId.WithUserAssignedClientId(clientId));
        }

        /// <summary>
        /// Creates an instance for user-assigned Managed Identity using Resource ID.
        /// </summary>
        /// <param name="resourceId">The full resource ID of the user-assigned identity.</param>
        /// <returns>A new VanillaMsiMtlsPop instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resourceId is null.</exception>
        /// <example>
        /// <code>
        /// var msi = VanillaMsiMtlsPop.CreateForUserAssignedByResourceId(
        ///     "/subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami");
        /// </code>
        /// </example>
        public static VanillaMsiMtlsPop CreateForUserAssignedByResourceId(string resourceId)
        {
            ArgumentNullException.ThrowIfNull(resourceId);
            return new VanillaMsiMtlsPop(ManagedIdentityId.WithUserAssignedResourceId(resourceId));
        }

        /// <summary>
        /// Creates an instance for user-assigned Managed Identity using Object ID.
        /// </summary>
        /// <param name="objectId">The object ID of the user-assigned identity (e.g., "ecb2ad92-3e30-4505-b79f-ac640d069f24").</param>
        /// <returns>A new VanillaMsiMtlsPop instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when objectId is null.</exception>
        public static VanillaMsiMtlsPop CreateForUserAssignedByObjectId(string objectId)
        {
            ArgumentNullException.ThrowIfNull(objectId);
            return new VanillaMsiMtlsPop(ManagedIdentityId.WithUserAssignedObjectId(objectId));
        }

        /// <summary>
        /// Acquires an mTLS PoP token for the specified resource.
        /// </summary>
        /// <param name="resource">The target resource (e.g., "https://graph.microsoft.com").</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The authentication result containing the PoP token and binding certificate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resource is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            string resource,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resource);
            ObjectDisposedException.ThrowIf(_disposed, this);

            AuthenticationResult result = await _app
                .AcquireTokenForManagedIdentity(resource)
                .WithMtlsProofOfPossession()
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Acquires an mTLS PoP token and calls the specified resource in a single operation.
        /// </summary>
        /// <param name="resourceUrl">The URL of the protected resource to call.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The response content from the resource as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resourceUrl is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        /// <remarks>
        /// This is a convenience method that combines token acquisition and resource calling.
        /// The resource scope is extracted from the resourceUrl for token acquisition.
        /// </remarks>
        public async Task<string> CallResourceWithPopAsync(
            string resourceUrl,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resourceUrl);
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Extract resource scope from URL (e.g., "https://graph.microsoft.com/v1.0/me" -> "https://graph.microsoft.com")
            var uri = new Uri(resourceUrl);
            string resource = $"{uri.Scheme}://{uri.Host}";

            // Acquire mTLS PoP token
            AuthenticationResult authResult = await AcquireTokenAsync(resource, cancellationToken)
                .ConfigureAwait(false);

            // Call resource with PoP token
            using var caller = new ResourceCaller(authResult);
            string response = await caller
                .CallResourceAsync(resourceUrl, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Disposes the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // MSAL application instances don't require explicit disposal
                _disposed = true;
            }
        }
    }
}
