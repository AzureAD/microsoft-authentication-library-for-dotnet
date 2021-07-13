// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Advanced;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Builder for AcquireTokenOnBehalfOf (OBO flow)
    /// See https://aka.ms/msal-net-on-behalf-of
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed class AcquireTokenOnBehalfOfParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<AcquireTokenOnBehalfOfParameterBuilder>
    {
        private AcquireTokenOnBehalfOfParameters Parameters { get; } = new AcquireTokenOnBehalfOfParameters();

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenOnBehalfOf;

        /// <inheritdoc />
        internal AcquireTokenOnBehalfOfParameterBuilder(IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor)
            : base(confidentialClientApplicationExecutor)
        {
        }

        internal static AcquireTokenOnBehalfOfParameterBuilder Create(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            IEnumerable<string> scopes, 
            UserAssertion userAssertion)
        {
            return new AcquireTokenOnBehalfOfParameterBuilder(confidentialClientApplicationExecutor)
                   .WithScopes(scopes)
                   .WithUserAssertion(userAssertion);
        }

        private AcquireTokenOnBehalfOfParameterBuilder WithUserAssertion(UserAssertion userAssertion)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithUserAssertion);
            Parameters.UserAssertion = userAssertion;
            return this;
        }

        /// <summary>
        /// Specifies if the x5c claim (public key of the certificate) should be sent to the STS.
        /// Sending the x5c enables application developers to achieve easy certificate roll-over in Azure AD:
        /// this method will send the public certificate to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation). For details see https://aka.ms/msal-net-sni
        /// </summary>
        /// <param name="withSendX5C"><c>true</c> if the x5c should be sent. Otherwise <c>false</c>.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenOnBehalfOfParameterBuilder WithSendX5C(bool withSendX5C)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithSendX5C, withSendX5C);
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <summary>
        /// Specifies if the client application should force refreshing the
        /// token from the user token cache. By default the token is taken from the
        /// the user token cache (forceRefresh=false)
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, ignore any access token in the user token cache
        /// and attempt to acquire new access token using the refresh token for the account
        /// if one is available. This can be useful in the case when the application developer wants to make
        /// sure that conditional access policies are applied immediately, rather than after the expiration of the access token.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks>Avoid unnecessarily setting <paramref name="forceRefresh"/> to <c>true</c> true in order to
        /// avoid negatively affecting the performance of your application</remarks>
        public AcquireTokenOnBehalfOfParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithForceRefresh, forceRefresh);
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        /// <summary>
        /// To help with resiliency, AAD Cached Credential Service (CCS) operates as an AAD backup.
        /// This will provide CCS with a routing hint to help improve performance during authentication.
        /// </summary>
        /// <param name="userObjectIdentifier">GUID which is unique to the user, parsed from the client_info.</param>
        /// <param name="tenantIdentifier">GUID format of the tenant ID, parsed from the client_info.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenOnBehalfOfParameterBuilder WithCcsRoutingHint(string userObjectIdentifier, string tenantIdentifier)
        {
            if (string.IsNullOrEmpty(userObjectIdentifier) || string.IsNullOrEmpty(tenantIdentifier))
            {
                return this;
            }

            var ccsRoutingHeader = new Dictionary<string, string>()
            {
                { Constants.CcsRoutingHintHeader, CoreHelpers.GetCcsClientInfoHint(userObjectIdentifier, tenantIdentifier) }
            };

            this.WithExtraHttpHeaders(ccsRoutingHeader);
            return this;
        }

        /// <summary>
        /// To help with resiliency, AAD Cached Credential Service (CCS) operates as an AAD backup.
        /// This will provide CCS with a routing hint to help improve performance during authentication.
        /// </summary>
        /// <param name="userName">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenOnBehalfOfParameterBuilder WithCcsRoutingHint(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                return this;
            }

            var ccsRoutingHeader = new Dictionary<string, string>()
            {
                { Constants.CcsRoutingHintHeader, CoreHelpers.GetCcsUpnHint(userName) }
            };

            this.WithExtraHttpHeaders(ccsRoutingHeader);
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ConfidentialClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenOnBehalfOf;
        }
    }
}
