// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
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
    /// Base class for confidential client application token request builders
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public abstract class AbstractConfidentialClientAcquireTokenParameterBuilder<T>
        : AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        internal AbstractConfidentialClientAcquireTokenParameterBuilder(IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor)
            : base(confidentialClientApplicationExecutor.ServiceBundle)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();
            ConfidentialClientApplicationExecutor = confidentialClientApplicationExecutor;
        }

        internal abstract Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();
            ValidateAndCalculateApiId();
            return ExecuteInternalAsync(cancellationToken);
        }

        internal IConfidentialClientApplicationExecutor ConfidentialClientApplicationExecutor { get; }

        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP), rather than a Bearer token. 
        ///  PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key, which MSAL can manage on Windows.
        ///  See https://aka.ms/msal-net-pop
        /// </summary>
        /// <param name="popAuthenticationConfiguration">Configuration properties used to construct a proof of possession request.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item> An Authentication header is automatically added to the request</item>
        /// <item> The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET, POST, etc.) and to the Uri (path and query, but not query parameters). </item>
        /// <item> MSAL creates, reads and stores a key in memory that will be cycled every 8 hours.</item>
        /// <item>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</item>
        /// </list>
        /// </remarks>
        public T WithProofOfPossession(PoPAuthenticationConfiguration popAuthenticationConfiguration)
        {
            ValidateUseOfExpirementalFeature();

            CommonParameters.PopAuthenticationConfiguration = popAuthenticationConfiguration ?? throw new ArgumentNullException(nameof(popAuthenticationConfiguration));

            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithPoPScheme);
            CommonParameters.AuthenticationScheme = new PoPAuthenticationScheme(CommonParameters.PopAuthenticationConfiguration, ServiceBundle);

            return this as T;
        }
    }
}
