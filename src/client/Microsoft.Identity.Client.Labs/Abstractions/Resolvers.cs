// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Labs
{
    /// <summary>
    /// Resolves user credentials (username and password) for a given tuple
    /// (<see cref="AuthType"/>, <see cref="CloudType"/>, <see cref="Scenario"/>).
    /// </summary>
    public interface IAccountResolver
    {
        /// <summary>
        /// Resolves the user credentials for the specified tuple.
        /// </summary>
        /// <param name="auth">The authentication style of the user.</param>
        /// <param name="cloud">The cloud environment.</param>
        /// <param name="scenario">The scenario (pool) for which the user is requested.</param>
        /// <param name="ct">An optional cancellation token.</param>
        /// <returns>
        /// A tuple <c>(Username, Password)</c> containing the credential values retrieved from Key Vault.
        /// </returns>
        Task<(string Username, string Password)> ResolveUserAsync(
            AuthType auth, CloudType cloud, Scenario scenario, CancellationToken ct = default);
    }

    /// <summary>
    /// Represents application credentials materialized from Key Vault secrets.
    /// Optional fields use empty strings or empty arrays when not configured.
    /// </summary>
    public sealed class AppCredentials
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppCredentials"/> class.
        /// </summary>
        /// <param name="clientId">The application (client) identifier.</param>
        /// <param name="clientSecret">The optional client secret used by confidential clients. Use <c>""</c> if not used.</param>
        /// <param name="pfxBytes">The optional PFX certificate content. Use <see cref="Array.Empty{T}"/> if not used.</param>
        /// <param name="pfxPassword">The optional password used to load the PFX certificate. Use <c>""</c> if not used.</param>
        public AppCredentials(
            string clientId,
            string clientSecret = "",
            byte[]? pfxBytes = null,
            string pfxPassword = "")
        {
            ClientId = clientId;
            ClientSecret = clientSecret ?? string.Empty;
            PfxBytes = pfxBytes ?? Array.Empty<byte>();
            PfxPassword = pfxPassword ?? string.Empty;
        }

        /// <summary>
        /// Gets the application (client) identifier.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the client secret. Empty string indicates "not configured".
        /// </summary>
        public string ClientSecret { get; }

        /// <summary>
        /// Gets the PFX certificate content. Empty array indicates "not configured".
        /// </summary>
        public byte[] PfxBytes { get; }

        /// <summary>
        /// Gets the password used to load the PFX certificate. Empty string indicates "not configured".
        /// </summary>
        public string PfxPassword { get; }
    }

    /// <summary>
    /// Resolves application credentials for a given tuple
    /// (<see cref="CloudType"/>, <see cref="Scenario"/>, <see cref="AppKind"/>).
    /// </summary>
    public interface IAppResolver
    {
        /// <summary>
        /// Resolves the application credentials for the specified tuple.
        /// </summary>
        /// <param name="cloud">The cloud environment.</param>
        /// <param name="scenario">The scenario (pool) for which the application is requested.</param>
        /// <param name="kind">The type of application to resolve.</param>
        /// <param name="ct">An optional cancellation token.</param>
        /// <returns>An <see cref="AppCredentials"/> instance containing the resolved values.</returns>
        Task<AppCredentials> ResolveAppAsync(
            CloudType cloud, Scenario scenario, AppKind kind, CancellationToken ct = default);
    }
}
