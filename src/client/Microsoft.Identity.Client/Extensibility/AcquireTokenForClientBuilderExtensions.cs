// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Extensibility
{

    /// <summary>
    /// Extensions for <see cref="AcquireTokenForClientBuilderExtensions"/> class
    /// </summary>
    public static class AcquireTokenForClientBuilderExtensions
    {
        /// <summary>
        /// Binds the token to a key in the cache. L2 cache keys contain the key id.
        /// No cryptographic operations is performed on the token.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="keyId">A key id to which the access token is associated. The token will not be retrieved from the cache unless the same key id is presented. Can be null.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static AcquireTokenForClientParameterBuilder WithKeyId(
            this AcquireTokenForClientParameterBuilder builder,
            string keyId)
        {
            if (string.IsNullOrEmpty(keyId))
            {
                throw new ArgumentNullException(nameof(keyId));
            }

            builder.ValidateUseOfExperimentalFeature();            
            
            if (!string.IsNullOrEmpty(keyId))
                builder.CommonParameters.AuthenticationScheme = new ExternalBoundTokenScheme(keyId);

            return builder;
        }
    }
}
