// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;

namespace Microsoft.Identity.Client.RP
{
    /// <summary>
    /// Resource Provider extensibility methods for AcquireTokenForClientParameterBuilder
    /// </summary>
    public static class AcquireTokenForClientParameterBuilderForResourceProviders
    {
        /// <summary>
        /// Configures the SDK to not retrieve a token from the cache if it matches the SHA256 hash 
        /// of the token configured. Similar to WithForceRefresh(bool) API, but instead of bypassing 
        /// the cache for all tokens, the cache bypass only occurs for 1 token
        /// </summary>
        /// <param name="builder">The existing AcquireTokenForClientParameterBuilder instance.</param>
        /// <param name="hash">
        /// A Base64-encoded SHA-256 hash of the token (UTF-8). For example:
        /// <c>Convert.ToBase64String(SHA256(Encoding.UTF8.GetBytes(accessToken)))</c>.
        /// </param>
        /// <returns>The builder to chain the .With methods.</returns>
        public static AcquireTokenForClientParameterBuilder WithAccessTokenSha256ToRefresh(
            this AcquireTokenForClientParameterBuilder builder,
            string hash)
        {
            if (!string.IsNullOrWhiteSpace(hash))
            {
                builder.Parameters.AccessTokenHashToRefresh = hash;
            }

            return builder;
        }
    }
}
