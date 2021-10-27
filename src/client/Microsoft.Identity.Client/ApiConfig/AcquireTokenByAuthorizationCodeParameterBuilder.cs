// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Advanced;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{

    /// <summary>
    /// Builder for AcquireTokenByAuthorizationCode
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed class AcquireTokenByAuthorizationCodeParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<AcquireTokenByAuthorizationCodeParameterBuilder>
    {
        private AcquireTokenByAuthorizationCodeParameters Parameters { get; } = new AcquireTokenByAuthorizationCodeParameters();

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenByAuthorizationCode;

        internal AcquireTokenByAuthorizationCodeParameterBuilder(IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor)
            : base(confidentialClientApplicationExecutor)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();
        }

        internal static AcquireTokenByAuthorizationCodeParameterBuilder Create(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            IEnumerable<string> scopes, 
            string authorizationCode)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            return new AcquireTokenByAuthorizationCodeParameterBuilder(confidentialClientApplicationExecutor)
                   .WithScopes(scopes).WithAuthorizationCode(authorizationCode);
        }

        private AcquireTokenByAuthorizationCodeParameterBuilder WithAuthorizationCode(string authorizationCode)
        {
            Parameters.AuthorizationCode = authorizationCode;
            return this;
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenByAuthorizationCode;
        }

        /// <inheritdoc />
        protected override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(Parameters.AuthorizationCode))
            {
                throw new ArgumentException("AuthorizationCode can not be null or whitespace", nameof(Parameters.AuthorizationCode));
            }

            if (Parameters.SendX5C == null)
            {
                Parameters.SendX5C = this.ServiceBundle.Config.SendX5C;
            }
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ConfidentialClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
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
        public AcquireTokenByAuthorizationCodeParameterBuilder WithSendX5C(bool withSendX5C)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithSendX5C);
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <summary>
        /// Used to secure authorization code grant via Proof of Key for Code Exchange (PKCE).
        /// See (https://tools.ietf.org/html/rfc7636) for more details.
        /// </summary>
        /// <param name="pkceCodeVerifier">A dynamically created cryptographically random key used to provide proof of possession for the authorization code.
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenByAuthorizationCodeParameterBuilder WithPkceCodeVerifier(string pkceCodeVerifier)
        {
            Parameters.PkceCodeVerifier = pkceCodeVerifier;
            return this;
        }

        /// <summary>
        /// To help with resiliency, the AAD backup authentication system operates as an AAD backup.
        /// This will provide backup authentication system with a routing hint to help improve performance during authentication.
        /// </summary>
        /// <param name="userObjectIdentifier">GUID which is unique to the user, parsed from the client_info.</param>
        /// <param name="tenantIdentifier">GUID format of the tenant ID, parsed from the client_info.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenByAuthorizationCodeParameterBuilder WithCcsRoutingHint(string userObjectIdentifier, string tenantIdentifier)
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
        /// To help with resiliency, the AAD backup authentication system operates as an AAD backup.
        /// This will provide backup authentication system with a routing hint to help improve performance during authentication.
        /// </summary>
        /// <param name="userName">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenByAuthorizationCodeParameterBuilder WithCcsRoutingHint(string userName)
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

        /// <summary>
        /// Requests an auth code for the frontend (SPA using MSAL.js for instance). 
        /// Also improves the end-to-end performance for applications to complete server-side and client-side authentication.
        /// </summary>
        /// <param name="spaCode"><c>true</c> if Hybrid Spa Auth Code should be returned,
        /// <c>false</c></param> otherwise.
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenByAuthorizationCodeParameterBuilder WithSpaAuthCode(bool spaCode = true)
        {
            Parameters.SpaCode = spaCode;

            return this;
        }
    }
}
