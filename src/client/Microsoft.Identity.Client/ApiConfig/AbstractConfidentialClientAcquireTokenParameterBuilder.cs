﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;

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
            ClientApplicationBase.GuardMobileFrameworks();
            ConfidentialClientApplicationExecutor = confidentialClientApplicationExecutor;
        }

        internal abstract Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <inheritdoc/>
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ClientApplicationBase.GuardMobileFrameworks();
            ValidateAndCalculateApiId();
            return ExecuteInternalAsync(cancellationToken);
        }

        /// <summary>
        /// Validates the parameters of the AcquireToken operation.        
        /// </summary>
        /// <exception cref="MsalClientException"></exception>
        protected override void Validate()
        {
            // Confidential client must have a credential
            if (ServiceBundle?.Config.ClientCredential == null &&
                CommonParameters.OnBeforeTokenRequestHandler == null &&
                ServiceBundle?.Config.AppTokenProvider == null 
                )
            {
                throw new MsalClientException(
                    MsalError.ClientCredentialAuthenticationTypeMustBeDefined,
                    MsalErrorMessage.ClientCredentialAuthenticationTypeMustBeDefined);
            }

            base.Validate();
        }

        internal IConfidentialClientApplicationExecutor ConfidentialClientApplicationExecutor { get; }

        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof-of-Possession token (PoP), rather than a Bearer token. 
        ///  PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key, which MSAL can manage on Windows.
        ///  See https://aka.ms/msal-net-pop
        /// </summary>
        /// <param name="popAuthenticationConfiguration">Configuration properties used to construct a Proof-of-Possession request.</param>
        /// <returns>The builder.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET, POST, etc.) and to the Uri (path and query, but not query parameters).</description></item>
        /// <item><description>MSAL creates, reads and stores a key in memory that will be cycled every 8 hours.</description></item>
        /// <item><description>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</description></item>
        /// </list>
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)] // Soft deprecate
        [Obsolete("WithProofOfPossession is deprecated. Use WithSignedHttpRequestProofOfPossession for SHR Proof-of-Possession functionality. " +
          "For more details and to learn about other Proof-of-Possession MSAL supports, see the MSAL documentation: https://aka.ms/msal-net-pop")]
        public T WithProofOfPossession(PoPAuthenticationConfiguration popAuthenticationConfiguration)
        {
            ValidateUseOfExperimentalFeature();

            CommonParameters.PopAuthenticationConfiguration = popAuthenticationConfiguration ?? throw new ArgumentNullException(nameof(popAuthenticationConfiguration));

            CommonParameters.AuthenticationOperation = new PopAuthenticationOperation(CommonParameters.PopAuthenticationConfiguration, ServiceBundle);

            return this as T;
        }

        /// <summary>
        /// Modifies the request to acquire a Signed HTTP Request (SHR) Proof-of-Possession (PoP) token, rather than a Bearer.
        /// SHR PoP tokens are bound to the HTTP request and to a cryptographic key, which MSAL manages on Windows.
        /// SHR PoP tokens are different from mTLS PoP tokens, which are used for Mutual TLS (mTLS) authentication. See <see href="https://aka.ms/mtls-pop"/> for details.
        /// </summary>
        /// <param name="popAuthenticationConfiguration">Configuration properties used to construct a Proof-of-Possession request.</param>
        /// <returns>The builder.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>The SHR PoP token is bound to the HTTP request, specifically to the HTTP method (for example, `GET` or `POST`) and to the URI path and query, excluding query parameters.</description></item>
        /// <item><description>MSAL creates, reads, and stores a key in memory that will be cycled every 8 hours.</description></item>
        /// <item><description>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</description></item>
        /// </list>
        /// </remarks>
        public T WithSignedHttpRequestProofOfPossession(PoPAuthenticationConfiguration popAuthenticationConfiguration)
        {
            ValidateUseOfExperimentalFeature();

            CommonParameters.PopAuthenticationConfiguration = popAuthenticationConfiguration ?? throw new ArgumentNullException(nameof(popAuthenticationConfiguration));

            CommonParameters.AuthenticationOperation = new PopAuthenticationOperation(CommonParameters.PopAuthenticationConfiguration, ServiceBundle);

            return this as T;
        }
    }
}
