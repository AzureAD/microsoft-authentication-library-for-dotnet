// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Client.Helpers
{
    /// <summary>
    /// Helper class for acquiring mTLS PoP tokens with either MSI or Confidential Client.
    /// </summary>
    public class MtlsPopTokenAcquirer : IDisposable
    {
        private readonly object _app;
        private readonly bool _isManagedIdentity;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance with a Managed Identity application.
        /// </summary>
        /// <param name="app">The managed identity application.</param>
        public MtlsPopTokenAcquirer(IManagedIdentityApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            _app = app;
            _isManagedIdentity = true;
        }

        /// <summary>
        /// Initializes a new instance with a Confidential Client application.
        /// </summary>
        /// <param name="app">The confidential client application.</param>
        public MtlsPopTokenAcquirer(IConfidentialClientApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            _app = app;
            _isManagedIdentity = false;
        }

        /// <summary>
        /// Acquires an mTLS PoP token for the specified scope(s).
        /// </summary>
        /// <param name="scopes">
        /// For MSI: Single scope string (e.g., "https://graph.microsoft.com/.default")
        /// For Confidential Client: Array of scopes (e.g., new[] { "https://vault.azure.net/.default" })
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authentication result with mTLS PoP token and binding certificate.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            object scopes,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(scopes);

            AuthenticationResult result;

            if (_isManagedIdentity)
            {
                var app = (IManagedIdentityApplication)_app;
                var scope = scopes as string;

                if (string.IsNullOrEmpty(scope))
                {
                    throw new ArgumentException(
                        "For Managed Identity, scopes must be a single scope string.", 
                        nameof(scopes));
                }

                result = await app
                    .AcquireTokenForManagedIdentity(scope)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var app = (IConfidentialClientApplication)_app;
                var scopeArray = scopes as string[];

                if (scopeArray == null || scopeArray.Length == 0)
                {
                    throw new ArgumentException(
                        "For Confidential Client, scopes must be a string array with at least one scope.", 
                        nameof(scopes));
                }

                result = await app
                    .AcquireTokenForClient(scopeArray)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// Disposes the underlying application if it's disposable.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            // Note: IManagedIdentityApplication and IConfidentialClientApplication
            // don't implement IDisposable in MSAL.NET 4.x, so no cleanup needed.
            _disposed = true;
        }
    }
}
