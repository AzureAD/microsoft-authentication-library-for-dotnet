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
    }
}
