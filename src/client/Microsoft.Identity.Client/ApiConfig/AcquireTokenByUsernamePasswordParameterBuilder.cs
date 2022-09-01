// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Parameter builder for the <see cref="IPublicClientApplication.AcquireTokenByUsernamePassword(IEnumerable{string}, string, string)"/>
    /// operation. See https://aka.ms/msal-net-up
    /// </summary>
    public sealed class AcquireTokenByUsernamePasswordParameterBuilder :
        AbstractPublicClientAcquireTokenParameterBuilder<AcquireTokenByUsernamePasswordParameterBuilder>
    {
        private AcquireTokenByUsernamePasswordParameters Parameters { get; } = new AcquireTokenByUsernamePasswordParameters();

        internal AcquireTokenByUsernamePasswordParameterBuilder(IPublicClientApplicationExecutor publicClientApplicationExecutor)
            : base(publicClientApplicationExecutor)
        {
        }

        internal static AcquireTokenByUsernamePasswordParameterBuilder Create(
            IPublicClientApplicationExecutor publicClientApplicationExecutor,
            IEnumerable<string> scopes,
            string username,
            string password)
        {
            return new AcquireTokenByUsernamePasswordParameterBuilder(publicClientApplicationExecutor)
                   .WithScopes(scopes).WithUsername(username).WithPassword(password);
        }

        /// <summary>
        /// Enables MSAL to read the federation metadata for a WS-Trust exchange from the provided input instead of acquiring it from an endpoint.
        /// This is only applicable for managed ADFS accounts. See https://aka.ms/MsalFederationMetadata.
        /// </summary>
        /// <param name="federationMetadata">Federation metadata in the form of XML.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenByUsernamePasswordParameterBuilder WithFederationMetadata(string federationMetadata)
        {
            Parameters.FederationMetadata = federationMetadata;
            return this;
        }

        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP), rather than a Bearer token. 
        ///  PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key, which MSAL can manage on Windows.
        ///  Note that only the host and path parts of the request URI will be bound.
        ///  See https://aka.ms/msal-net-pop
        /// </summary>
        /// <param name="httpMethod">The HTTP method ("GET", "POST" etc.) method that will be bound to the token. Leave null and the POP token will not be bound to the method.
        /// Corresponds to the "m" part of the a signed HTTP request.</param>
        /// <param name="requestUri">The URI to bind the signed HTTP request to.</param>
        /// <param name="nonce">Nonce of the protected resource (RP) which will be published as part of the WWWAuthenticate header associated with a 401 HTTP response
        /// or as part of the AuthorityInfo header associated with 200 response. Set it here to make it part of the Signed HTTP Request part of the POP token.</param>
        /// <returns>The builder.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>An Authentication header is automatically added to the request</description></item>
        /// <item><description>The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET, POST, etc.) and to the Uri (path and query, but not query parameters).</description></item>
        /// <item><description>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</description></item>
        /// </list>
        /// </remarks>
#if iOS || ANDROID || WINDOWS_UWP
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public AcquireTokenByUsernamePasswordParameterBuilder WithProofOfPossession(string nonce, HttpMethod httpMethod, Uri requestUri)
        {
            ValidateUseOfExperimentalFeature();
            ClientApplicationBase.GuardMobileFrameworks();

            if (!ServiceBundle.Config.IsBrokerEnabled)
            {
                throw new MsalClientException(MsalError.BrokerRequiredForPop, MsalErrorMessage.BrokerRequiredForPop);
            }

            var broker = ServiceBundle.PlatformProxy.CreateBroker(ServiceBundle.Config, null);

            if (!broker.IsPopSupported)
            {
                throw new MsalClientException(MsalError.BrokerDoesNotSupportPop, MsalErrorMessage.BrokerDoesNotSupportPop);
            }

            if (string.IsNullOrEmpty(nonce))
            {
                throw new ArgumentNullException(nameof(nonce));
            }

            PoPAuthenticationConfiguration popConfig = new PoPAuthenticationConfiguration(requestUri);

            popConfig.Nonce = nonce;
            popConfig.HttpMethod = httpMethod;

            CommonParameters.PopAuthenticationConfiguration = popConfig;
            CommonParameters.AuthenticationScheme = new PopBrokerAuthenticationScheme();

            return this;
        }

        private AcquireTokenByUsernamePasswordParameterBuilder WithUsername(string username)
        {
            Parameters.Username = username;
            return this;
        }

        private AcquireTokenByUsernamePasswordParameterBuilder WithPassword(string password)
        {
            Parameters.Password = password;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return PublicClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenByUsernamePassword;
        }
    }
}
