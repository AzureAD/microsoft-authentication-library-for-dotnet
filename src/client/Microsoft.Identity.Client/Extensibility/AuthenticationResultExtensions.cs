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
        /// The refresh token string, or <c>null</c> if the token response did not include a refresh token
        /// (e.g., client credentials flow, or when the token was served from cache).
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
