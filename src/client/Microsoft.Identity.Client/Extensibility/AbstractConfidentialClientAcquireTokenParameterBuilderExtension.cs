// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensions for <see cref="AcquireTokenForClientParameterBuilder"/>
    /// </summary>
    public static partial class AbstractConfidentialClientAcquireTokenParameterBuilderExtension
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
            builder.ValidateUseOfExperimentalFeature();
            builder.CommonParameters.OnBeforeTokenRequestHandler = onBeforeTokenRequestHandler;

            return builder;
        }
    }   
}
