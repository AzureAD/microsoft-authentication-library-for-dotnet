// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensibility
{

    /// <summary>
    /// Extensions for all AcquireToken methods
    /// </summary>
    public static class AbstractConfidentialClientAcquireTokenParameterBuilderExtension
    {
        /// <summary>
        /// Intervenes in the request pipeline, by executing a user provided delegate before MSAL makes the token request. 
        /// The delegate can modify the request payload by adding or removing  body parameters and headers. <see cref="OnBeforeTokenRequestData"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder to chain options to</param>
        /// <param name="onBeforeTokenRequestHandler">An async delegate which gets invoked just before MSAL makes a token request</param>
        /// <returns>The builder to chain other options to.</returns>
        public static AbstractAcquireTokenParameterBuilder<T> OnBeforeTokenRequest<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder, 
            Func<OnBeforeTokenRequestData, Task> onBeforeTokenRequestHandler) 
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (builder.CommonParameters.OnBeforeTokenRequestHandler != null && onBeforeTokenRequestHandler != null)
            {
                throw new InvalidOperationException("Cannot set OnBeforeTokenRequest handler twice.");
            }

            builder.CommonParameters.OnBeforeTokenRequestHandler = onBeforeTokenRequestHandler;

            return builder;
        }

        /// <summary>
        /// Binds the token to a key in the cache.No cryptographic operations is performed on the token.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder to chain options to</param>
        /// <param name="keyId">A key id to which the access token is associated. The token will not be retrieved from the cache unless the same key id is presented. Can be null.</param>
        /// <param name="expectedTokenTypeFromAad">AAD issues several types of bound tokens. MSAL checks the token type, which needs to match the value set by ESTS. Normal POP tokens have this as "pop"</param>
        /// <returns>the builder</returns>
        public static AbstractAcquireTokenParameterBuilder<T> WithProofOfPosessionKeyId<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder,
            string keyId,
            string expectedTokenTypeFromAad = "Bearer")
            where T : AbstractAcquireTokenParameterBuilder<T>
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
        /// Enables client applications to provide a custom authentication operation to be used in the token acquisition request.
        /// </summary>
        /// <param name="builder">The builder to chain options to</param>
        /// <param name="authenticationExtension">The implementation of the authentication operation.</param>
        /// <returns></returns>
        public static AbstractAcquireTokenParameterBuilder<T> WithAuthenticationExtension<T>(
           this AbstractAcquireTokenParameterBuilder<T> builder,
           MsalAuthenticationExtension authenticationExtension)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (builder.CommonParameters.OnBeforeTokenRequestHandler != null && authenticationExtension.OnBeforeTokenRequestHandler != null)
            {
                throw new InvalidOperationException("Cannot set both an AuthenticaitonOperation and an OnBeforeTokenRequestHandler");
            }

            builder.CommonParameters.OnBeforeTokenRequestHandler = authenticationExtension.OnBeforeTokenRequestHandler;

            if (authenticationExtension.AuthenticationOperation != null)
                builder.WithAuthenticationOperation(authenticationExtension.AuthenticationOperation);

            if (authenticationExtension.AdditionalCacheParameters != null)
                builder.WithAdditionalCacheParameters(authenticationExtension.AdditionalCacheParameters);

            return builder;
        }
        
        /// <summary>
        /// Specifies additional parameters acquired from authentication responses to be cached with the access token that are normally not included in the cache object.
        /// these values can be read from the <see cref="AuthenticationResult.AdditionalResponseParameters"/> parameter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder to chain options to</param>
        /// <param name="cacheParameters">Additional parameters to cache</param>
        /// <returns></returns>
        public static AbstractAcquireTokenParameterBuilder<T> WithAdditionalCacheParameters<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder, 
            IEnumerable<string> cacheParameters)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (cacheParameters != null && !cacheParameters.Any())
            {
                return builder;
            }

            builder.ValidateUseOfExperimentalFeature();

            //Check if the cache parameters are already initialized, if so, add to the existing list
            if (builder.CommonParameters.AdditionalCacheParameters != null)
            {
                builder.CommonParameters.AdditionalCacheParameters.AddRange(cacheParameters);
            }
            else
            {
                builder.CommonParameters.AdditionalCacheParameters = cacheParameters.ToList<string>();
            }
            return builder;
        }
    }   
}
