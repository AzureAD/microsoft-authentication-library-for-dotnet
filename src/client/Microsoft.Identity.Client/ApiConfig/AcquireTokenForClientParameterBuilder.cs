// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Builder for AcquireTokenForClient (used in client credential flows, in daemon applications).
    /// See https://aka.ms/msal-net-client-credentials
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed class AcquireTokenForClientParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<AcquireTokenForClientParameterBuilder>
    {
        private AcquireTokenForClientParameters Parameters { get; } = new AcquireTokenForClientParameters();

        /// <inheritdoc />
        internal AcquireTokenForClientParameterBuilder(IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor)
            : base(confidentialClientApplicationExecutor)
        {
        }

        internal static AcquireTokenForClientParameterBuilder Create(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            IEnumerable<string> scopes)
        {
            return new AcquireTokenForClientParameterBuilder(confidentialClientApplicationExecutor).WithScopes(scopes);
        }

        /// <summary>
        /// Specifies if the token request will ignore the access token in the application token cache
        /// and will attempt to acquire a new access token using client credentials.
        /// By default the token is taken from the application token cache (forceRefresh=false)
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, the request will ignore the token cache. The default is <c>false</c>
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenForClientParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        /// <summary>
        /// Applicable to first-party applications only, this method also allows to specify 
        /// if the <see href="https://datatracker.ietf.org/doc/html/rfc7517#section-4.7">x5c claim</see> should be sent to Azure AD.
        /// Sending the x5c enables application developers to achieve easy certificate roll-over in Azure AD:
        /// this method will send the certificate chain to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation). For details see https://aka.ms/msal-net-sni
        /// </summary>
        /// <param name="withSendX5C"><c>true</c> if the x5c should be sent. Otherwise <c>false</c>.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenForClientParameterBuilder WithSendX5C(bool withSendX5C)
        {
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <summary>
        /// Specifies to use the Managed Identity to fetch the token from the Managed Identity Endpoint.
        /// For more details see https://aka.ms/msal-managed-identity.
        /// </summary>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenForClientParameterBuilder WithManagedIdentity(string userAssignedClientIdOrResourceId = null)
        {
            ValidateUseOfExperimentalFeature("ManagedIdentity");

            ServiceBundle.Config.UseManagedIdentity = true;

            if (!userAssignedClientIdOrResourceId.IsNullOrEmpty() && Guid.TryParse(userAssignedClientIdOrResourceId, out _))
            {
                ServiceBundle.Config.MIUserAssignedClientId = userAssignedClientIdOrResourceId;
            }
            else
            {
                ServiceBundle.Config.MIUserAssignedResourceId = userAssignedClientIdOrResourceId;
            }

            return this;
        }

        /// <summary>
        /// Please use WithAzureRegion on the ConfidentialClientApplicationBuilder object
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use WithAzureRegion on the ConfidentialClientApplicationBuilder object", true)]
        public AcquireTokenForClientParameterBuilder WithAzureRegion(bool useAzureRegion)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Please use WithAzureRegion on the ConfidentialClientApplicationBuilder object
        /// </summary>        
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use WithAzureRegion on the ConfidentialClientApplicationBuilder object", true)]
        public AcquireTokenForClientParameterBuilder WithPreferredAzureRegion(bool useAzureRegion = true, string regionUsedIfAutoDetectFails = "", bool fallbackToGlobal = true)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ConfidentialClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();
            if (Parameters.SendX5C == null)
            {
                Parameters.SendX5C = this.ServiceBundle.Config.SendX5C;
            }
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenForClient;
        }
    }
}
