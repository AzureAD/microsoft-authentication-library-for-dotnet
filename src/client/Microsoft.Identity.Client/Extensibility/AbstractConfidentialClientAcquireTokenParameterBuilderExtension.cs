// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// TODO: design for 2 things - Test User and CDT
    /// </summary>
    public class AddIn
    {
        /// <summary>
        /// 
        /// </summary>
        public Func<OnBeforeTokenRequestData, Task> OnBeforeTokenRequestHandler { get; set; }

        /// <summary>
        /// Changes the 
        /// </summary>
        /// TODO: guidance on how this interacts with OnBeforeTokenRequestHandler
        public IAuthenticationScheme AuthenticationScheme { get; set; }

        /// <summary>
        /// When the token endpoint responds with a token, it may include additional properties in the response. This list instructs MSAL to save the properties in the token cache. 
        /// The properties will be returned as part of the <see cref="AuthenticationResult.AdditionalResponseParameters"/> 
        /// </summary>
        /// <remarks>Currently supports only key value properties </remarks>  // TODO: need to model JObject etc, but probably as string
        public IReadOnlyList<string> AdditionalAccessTokenPropertiesToCache { get; set; }  //TODO: bogavril - implement this
    }

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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="addIn"></param>
        /// <returns></returns>
        public static AbstractAcquireTokenParameterBuilder<T> WithAddIn<T>( // TODO: bogavril - support a list of add-ins ? 
           this AbstractAcquireTokenParameterBuilder<T> builder,
           AddIn addIn)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            if (builder.CommonParameters.OnBeforeTokenRequestHandler != null && addIn.OnBeforeTokenRequestHandler != null)
            {
                throw new InvalidOperationException("Cannot set both an add-in and an OnBeforeTokenRequestHandler");
            }

            builder.CommonParameters.OnBeforeTokenRequestHandler = addIn.OnBeforeTokenRequestHandler;

            if (addIn.AuthenticationScheme != null)
                builder.WithAuthenticationScheme(addIn.AuthenticationScheme);

            // TODO: bogavril - AdditionalAccessTokenPropertiesToCache needs implementation

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
            builder.CommonParameters.AuthenticationScheme = new ExternalBoundTokenScheme(keyId, expectedTokenTypeFromAad);

            return builder;
        }
    }
}
