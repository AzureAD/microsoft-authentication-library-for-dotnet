// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// 
    /// </summary>
    public static class AcquireTokenForClientBuilderExtensions
    {
        /// <summary>
        /// Specifies additional cache key components to use when caching and retrieving tokens.
        /// </summary>
        /// <param name="cacheKeyComponents">The list of additional cache key components.</param>
        /// <param name="builder"></param>
        /// <returns>The builder.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>This api can be used to associate certificate key identifiers along with other keys with a particular token.</description></item>
        /// <item><description>In order for the tokens to be successfully retrieved from the cache, all components used to cache the token must be provided.</description></item>
        /// </list>
        /// </remarks>
        public static AcquireTokenForClientParameterBuilder WithAdditionalCacheKeyComponents(this AcquireTokenForClientParameterBuilder builder,
            IDictionary<string, string> cacheKeyComponents)
        {
            builder.ValidateUseOfExperimentalFeature();

            if (cacheKeyComponents == null || cacheKeyComponents.Count == 0)
            {
                //no-op
                return builder;
            }

            builder.CommonParameters.CacheKeyComponents = new SortedDictionary<string, string>(cacheKeyComponents);

            return builder;
        }

        /// <summary>
        /// Binds the token to a key in the cache. L2 cache keys contain the key id.
        /// No cryptographic operations is performed on the token.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="keyId">A key id to which the access token is associated. The token will not be retrieved from the cache unless the same key id is presented. Can be null.</param>
        /// <param name="expectedTokenTypeFromAad">AAD issues several types of bound tokens. MSAL checks the token type, which needs to match the value set by ESTS. Normal POP tokens have this as "pop"</param>
        /// <returns>the builder</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4789        
        public static AcquireTokenForClientParameterBuilder WithProofOfPosessionKeyId( 
            this AcquireTokenForClientParameterBuilder builder,
            string keyId,
            string expectedTokenTypeFromAad = "Bearer")
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentNullException(nameof(keyId));
            }

            builder.ValidateUseOfExperimentalFeature();
            builder.CommonParameters.AuthenticationOperation = new ExternalBoundTokenScheme(keyId, expectedTokenTypeFromAad);

            return builder;
        }
    }
}
