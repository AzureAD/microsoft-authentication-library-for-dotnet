// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// 
    /// </summary>
    public static class AcquireTokenForClientBuilderExtensions
    {
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

        /// <summary>
        /// Add extra body parameters to the token request. These parameters are added to the cache key to associate these parameters with the acquired token.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="extrabodyparams">List of additional body parameters</param>
        /// <returns></returns>
        public static AcquireTokenForClientParameterBuilder WithExtraBodyParameters(
            this AcquireTokenForClientParameterBuilder builder, 
            Dictionary<string, Func<CancellationToken, Task<string>>> extrabodyparams)
        {
            builder.ValidateUseOfExperimentalFeature();
            if (extrabodyparams == null || extrabodyparams.Count == 0)
            {
                return builder;
            }
            builder.OnBeforeTokenRequest(async (data) =>
            {
                foreach (var param in extrabodyparams)
                {
                    if (param.Value != null)
                    {
                        data.BodyParameters.Add(param.Key, await param.Value(data.CancellationToken).ConfigureAwait(false));
                    }
                }
            });

            builder.WithAdditionalCacheKeyComponents(extrabodyparams);
            return builder;
        }

        /// <summary>
        /// Specifies extra claims to be included in the client assertion. 
        /// These claims will be merged with default claims when the client assertion is generated.
        /// This lets higher level APIs like Microsoft.Identity.Web provide additional claims for the client assertion.
        /// Important: tokens are associated with the extra client assertion claims, which impacts cache lookups.
        /// This is an extensibility API and should not be used by applications directly.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="claimsToSign">Additional claims in JSON format to be signed in the client assertion.</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <exception cref="ArgumentNullException">Thrown when claimsToSign is null or whitespace.</exception>
        public static AcquireTokenForClientParameterBuilder WithExtraClientAssertionClaims(
            this AcquireTokenForClientParameterBuilder builder,
            string claimsToSign)
        {

            if (string.IsNullOrWhiteSpace(claimsToSign))
            {
                throw new ArgumentNullException(nameof(claimsToSign));
            }

            builder.CommonParameters.ExtraClientAssertionClaims = claimsToSign;

            // Add the extra claims to the cache key so different claims result in different cache entries
            var cacheKey = new SortedList<string, Func<CancellationToken, Task<string>>>
            {
                { "extra_client_assertion_claims", (CancellationToken ct) => Task.FromResult(claimsToSign) }
            };

            builder.WithAdditionalCacheKeyComponents(cacheKey);

            return builder;
        }
    }
}
