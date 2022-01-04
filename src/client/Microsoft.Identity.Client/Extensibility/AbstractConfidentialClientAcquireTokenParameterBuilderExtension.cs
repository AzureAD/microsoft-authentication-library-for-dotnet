// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensions for <see cref="AcquireTokenForClientParameterBuilder"/>
    /// </summary>
    public static class AbstractConfidentialClientAcquireTokenParameterBuilderExtension
    {
        /// <summary>
        /// Overrides the client credentials parameters (e.g. client_assertion) 
        /// </summary>
        /// <param name="clientAssertionProvider">A provider which you define to return the key / value parameters which will be added to the token endpoint</param>
        /// <param name="builder">Builder to chain config options to</param>
        /// <returns>The builder</returns>
        /// <remarks>
        /// This is an advanced API. See https://docs.microsoft.com/en-gb/azure/active-directory/develop/msal-net-client-assertions for 
        /// the common use cases such as using a certificate.
        /// </remarks>
        public static AbstractConfidentialClientAcquireTokenParameterBuilder<T> WithClientAssertion<T>
            (this AbstractConfidentialClientAcquireTokenParameterBuilder<T> builder,
            IClientAssertionProvider clientAssertionProvider)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            builder.ValidateUseOfExperimentalFeature();
            builder.CommonParameters.ClientAssertionParametersProvider = clientAssertionProvider;

            return builder;
        }
    }
}
