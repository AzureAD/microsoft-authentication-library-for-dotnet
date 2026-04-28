// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extension methods for <see cref="AuthenticationResult"/>.
    /// </summary>
    public static class AuthenticationResultExtensions
    {
        /// <summary>
        /// Returns the refresh token from the authentication result, if available.
        /// This is intended for advanced scenarios where the caller manages its own token cache,
        /// for example when using <see cref="CacheOptions.DisableInternalCache"/>.
        /// </summary>
        /// <param name="result">The authentication result.</param>
        /// <returns>
        /// The refresh token string if the result is from a confidential client flow and the token
        /// response included a refresh token; <c>null</c> otherwise. Refresh tokens are not exposed
        /// for public client flows, client credentials, managed identity, or when the token was
        /// served from cache. For the normal (non-long-running) On-Behalf-Of flow, MSAL intentionally
        /// clears the refresh token, so this method will also return <c>null</c>.
        /// </returns>
        /// <remarks>
        /// Refresh tokens are long-lived credentials. Store them securely and never expose them to end users or untrusted code.
        /// </remarks>
        public static string GetRefreshToken(this AuthenticationResult result)
        {
            return result?.RefreshToken;
        }
    }
}
