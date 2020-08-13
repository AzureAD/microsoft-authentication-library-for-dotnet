// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// Base class for confidential client application token request builders
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractConfidentialClientAcquireTokenParameterBuilder<T>
        : AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        internal AbstractConfidentialClientAcquireTokenParameterBuilder(IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor)
        {
            ConfidentialClientApplicationExecutor = confidentialClientApplicationExecutor;
        }

        internal abstract Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateAndCalculateApiId();
            return ExecuteInternalAsync(cancellationToken);
        }

        internal IConfidentialClientApplicationExecutor ConfidentialClientApplicationExecutor { get; }

#if DESKTOP || NET_CORE
        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP), rather than a Bearer token. 
        ///  PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key, which MSAL can manage on Windows.
        ///  See https://aka.ms/msal-net-pop
        /// </summary>
        /// <param name="httpRequestMessage">An HTTP request to the protected resource which requires a PoP token. The PoP token will be cryptographically bound to the request.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</item>
        /// <item> An Authentication header is automatically added to the request</item>
        /// <item> The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET, POST, etc.) and to the Uri (path and query, but not query parameters). </item>
        /// <item> MSAL creates, reads and stores a key in memory that will be cycled every 8 hours.</item>
        /// </list>
        /// </remarks>
        public T WithProofOfPosession(HttpRequestMessage httpRequestMessage)
        {
            var defaultCryptoProvider = this.ConfidentialClientApplicationExecutor.ServiceBundle.PlatformProxy.GetDefaultPoPCryptoProvider();
            return WithProofOfPosession(httpRequestMessage, defaultCryptoProvider);
        }

        // Allows testing the PoP flow with any crypto. Consider making this public.
        internal T WithProofOfPosession(HttpRequestMessage httpRequestMessage, IPoPCryptoProvider popCryptoProvider)
        {
            if (!this.ConfidentialClientApplicationExecutor.ServiceBundle.Config.ExperimentalFeaturesEnabled)
            {
                throw new MsalClientException(
                    MsalError.ExperimentalFeature,
                    MsalErrorMessage.ExperimentalFeature(nameof(WithProofOfPosession)));
            }

            if (httpRequestMessage is null)
            {
                throw new ArgumentNullException(nameof(httpRequestMessage));
            }

            if (popCryptoProvider == null)
            {
                throw new ArgumentNullException(nameof(popCryptoProvider));
            }

            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithPoPScheme);
            CommonParameters.AuthenticationScheme = new PoPAuthenticationScheme(httpRequestMessage, popCryptoProvider);

            return this as T;
        }
#endif
    }
#endif
}
