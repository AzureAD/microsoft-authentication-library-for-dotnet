// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// A delegate which return returns the key - value parameters which will be added to the token endpoint when used in WithClientAssertion.
    /// Implementers may use the input parameters clientId and tokenEndpoint which MSAL computes.
    /// </summary>
    /// <param name="clientId">The client id configured in the application. May be used as issuer claim in a JWT assertion.</param>
    /// <param name="tokenEndpoint">The token endpoint where the request will be made. May be used as audience claim in a JWT assertion.</param>
    /// <param name="cancellationToken">The cancellation token used by the token request, if any.</param>
    /// <returns></returns>
    /// <remarks>For a certificate based JWT assertion, see For the JWT-Bearer assertion format see https://docs.microsoft.com/en-gb/azure/active-directory/develop/active-directory-certificate-credentials#assertion-format</remarks>
    public delegate Task<IReadOnlyDictionary<string, string>> ClientAssertionProviderAsync(
        string clientId, 
        string tokenEndpoint, 
        CancellationToken cancellationToken);

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
            ClientAssertionProviderAsync clientAssertionProvider)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            builder.ValidateUseOfExperimentalFeature();
            builder.CommonParameters.ClientAssertionParametersProvider = clientAssertionProvider;

            return builder;
        }
    }

}
