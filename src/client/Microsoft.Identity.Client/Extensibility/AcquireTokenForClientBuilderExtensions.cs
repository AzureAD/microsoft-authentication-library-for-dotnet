// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        /// Add extra body parameters to the token request (synchronous version).
        /// These parameters are added to the cache key to associate these parameters with the acquired token.
        /// Use this overload when parameters are known synchronously to avoid async overhead.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="extrabodyparams">Dictionary of static string values for additional body parameters</param>
        /// <returns>The builder for method chaining</returns>
        /// <remarks>
        /// This synchronous overload is optimized for the common case where parameter values are statically known
        /// and do not require async computation. Parameters are added directly to the HTTP request without any
        /// async wrapping on the hot path, reducing memory allocation and GC pressure.
        /// 
        /// Cache key compatibility: Sync parameters with identical key/value pairs will produce the same cache keys
        /// as async parameters with identical values, enabling transparent interoperability between sync and async APIs.
        /// </remarks>
        public static AcquireTokenForClientParameterBuilder WithExtraBodyParameters(
            this AcquireTokenForClientParameterBuilder builder,
            Dictionary<string, string> extrabodyparams)
        {
            builder.ValidateUseOfExperimentalFeature();
            if (extrabodyparams == null || extrabodyparams.Count == 0)
            {
                return builder;
            }

            // Add parameters directly to HTTP request - SYNC, no async overhead
            builder.OnBeforeTokenRequest((data) =>
            {
                foreach (var param in extrabodyparams)
                {
                    if (!string.IsNullOrWhiteSpace(param.Key) && !string.IsNullOrWhiteSpace(param.Value))
                    {
                        data.BodyParameters.Add(param.Key, param.Value);
                    }
                }
                return Task.CompletedTask;
            });

            // Add to cache key components - uses new sync overload
            // No async wrapper allocation on hot path
            builder.WithAdditionalCacheKeyComponents(extrabodyparams);

            return builder;
        }
    }
}
