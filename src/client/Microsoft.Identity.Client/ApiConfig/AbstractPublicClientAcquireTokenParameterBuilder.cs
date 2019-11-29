// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PoP;
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
        {
            PublicClientApplicationExecutor = publicClientApplicationExecutor;
        }

#if DESKTOP
        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP), rather than a Bearer token. 
        ///  Pop specific cypto key pair is generated and stored by MSAL.NET on behalf of the user. See https://aka.ms/msal-net-pop
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</item>
        /// <item> Add the PoP token in an Authorization header, just like a bearer token. See <seealso cref="AuthenticationResult.CreateAuthorizationHeader"/> for details.</item>
        /// <item> The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET or POST) and to the Uri (path and query). </item>
        /// </list>
        /// </remarks>
        public T WithPoPAuthenticationScheme(Uri requestUri, HttpMethod httpMethod) 
        {
            var defaultCryptoProvider = this.PublicClientApplicationExecutor.ServiceBundle.PlatformProxy.GetDefaultPoPCryptoProvider();
            return WithPoPAuthenticationScheme(requestUri, httpMethod, defaultCryptoProvider);
        }

        // Allows testing the PoP flow with any crypto. Consider making this public.
        internal T WithPoPAuthenticationScheme(Uri requestUri, HttpMethod httpMethod, IPoPCryptoProvider popCryptoProvider) 
        {
            if (requestUri is null)
            {
                throw new ArgumentNullException(nameof(requestUri));
            }

            if (httpMethod is null)
            {
                throw new ArgumentNullException(nameof(httpMethod));
            }

            if (popCryptoProvider == null)
            {
                throw new ArgumentNullException(nameof(popCryptoProvider));
            }

            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithPoPScheme);
            CommonParameters.AuthenticationScheme = new PoPAuthenticationScheme(requestUri, httpMethod,  popCryptoProvider);

            return this as T;
        }
#endif

        internal abstract Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateAndCalculateApiId();
            return ExecuteInternalAsync(cancellationToken);
        }

        /// <summary>
        /// </summary>
        internal IPublicClientApplicationExecutor PublicClientApplicationExecutor { get; }
    }
}
