// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Base class for public client application token request builders
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractPublicClientAcquireTokenParameterBuilder<T>
        : AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        internal AbstractPublicClientAcquireTokenParameterBuilder(IPublicClientApplicationExecutor publicClientApplicationExecutor)
            : base(publicClientApplicationExecutor.ServiceBundle)
        {
            PublicClientApplicationExecutor = publicClientApplicationExecutor;
        }

        internal abstract Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateAndCalculateApiId();
            return ExecuteInternalAsync(cancellationToken);
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
        /// <item><description>MSAL creates, reads and stores a key in memory that will be cycled every 8 hours.</description></item>
        /// <item><description>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</description></item>
        /// </list>
        /// </remarks>
#if iOS || ANDROID || WINDOWS_UWP
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public T WithProofOfPossession(HttpMethod httpMethod, Uri requestUri, string nonce)
        {
            ClientApplicationBase.GuardMobileFrameworks();
            PoPAuthenticationConfiguration popConfig = new PoPAuthenticationConfiguration(requestUri ?? throw new ArgumentNullException(nameof(requestUri)));
            popConfig.HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
            popConfig.Nonce = nonce ?? throw new ArgumentNullException(nameof(nonce));

            CommonParameters.PopAuthenticationConfiguration = popConfig;
            var broker = ServiceBundle.PlatformProxy.CreateBroker(ServiceBundle.Config, null);

            if (CommonParameters.PopAuthenticationConfiguration != null)
            {
                if (!broker.IsPopSupported)
                {
                    throw new MsalClientException(MsalError.BrokerDoesNotSupportPop, MsalErrorMessage.BrokerDoesNotSupportPop);
                }
                
                if (!ServiceBundle.Config.IsBrokerEnabled)
                {
                    throw new MsalClientException(MsalError.BrokerRequiredForPop, MsalErrorMessage.BrokerRequiredForPop);
                }
            }

            return this as T;
        }

        /// <summary>
        /// </summary>
        internal IPublicClientApplicationExecutor PublicClientApplicationExecutor { get; }
    }
}
