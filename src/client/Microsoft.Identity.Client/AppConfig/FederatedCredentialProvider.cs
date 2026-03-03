// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Factory methods to create common <see cref="AssertionRequestOptions"/> delegates for use with
    /// <see cref="IByUserFederatedIdentityCredential.AcquireTokenByUserFederatedIdentityCredential"/>.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public static class FederatedCredentialProvider
    {
        private const string DefaultAudience = "api://AzureADTokenExchange/.default";

        /// <summary>
        /// Creates an assertion provider delegate that acquires a token from a Managed Identity.
        /// </summary>
        /// <param name="managedIdentityId">
        /// The managed identity to use. Use <see cref="ManagedIdentityId.SystemAssigned"/> for system-assigned
        /// or <see cref="ManagedIdentityId.WithUserAssignedClientId(string)"/> for user-assigned.
        /// </param>
        /// <param name="audience">
        /// The audience (resource) for which the managed identity token is acquired.
        /// Defaults to <c>api://AzureADTokenExchange/.default</c>.
        /// </param>
        /// <returns>A delegate that acquires a managed identity token and returns its access token string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="managedIdentityId"/> is null.</exception>
        public static Func<AssertionRequestOptions, Task<string>> FromManagedIdentity(
            ManagedIdentityId managedIdentityId,
            string audience = DefaultAudience)
        {
            if (managedIdentityId == null)
            {
                throw new ArgumentNullException(nameof(managedIdentityId));
            }

            if (audience == null)
            {
                throw new ArgumentNullException(nameof(audience));
            }

            // Eagerly build the ManagedIdentityApplication
            var miApp = ManagedIdentityApplicationBuilder.Create(managedIdentityId).Build();

            return async (options) =>
            {
                var result = await miApp
                    .AcquireTokenForManagedIdentity(audience)
                    .ExecuteAsync(options.CancellationToken)
                    .ConfigureAwait(false);

                return result.AccessToken;
            };
        }

        /// <summary>
        /// Creates an assertion provider delegate that acquires a token from a Confidential Client Application.
        /// </summary>
        /// <param name="cca">The confidential client application to use for token acquisition.</param>
        /// <param name="audience">
        /// The audience (scope) for which the confidential client acquires a token.
        /// Defaults to <c>api://AzureADTokenExchange/.default</c>.
        /// </param>
        /// <returns>A delegate that acquires a confidential client token and returns its access token string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cca"/> is null.</exception>
        public static Func<AssertionRequestOptions, Task<string>> FromConfidentialClient(
            IConfidentialClientApplication cca,
            string audience = DefaultAudience)
        {
            if (cca == null)
            {
                throw new ArgumentNullException(nameof(cca));
            }

            if (audience == null)
            {
                throw new ArgumentNullException(nameof(audience));
            }

            return async (options) =>
            {
                var result = await cca
                    .AcquireTokenForClient(new[] { audience })
                    .ExecuteAsync(options.CancellationToken)
                    .ConfigureAwait(false);

                return result.AccessToken;
            };
        }
    }
}
