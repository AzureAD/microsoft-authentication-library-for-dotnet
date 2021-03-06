﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

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

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenForClient;

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
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithForceRefresh, forceRefresh);
            Parameters.ForceRefresh = forceRefresh;
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
        public AcquireTokenForClientParameterBuilder WithSendX5C(bool withSendX5C)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithSendX5C, withSendX5C);
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <summary>
        /// Specifies if the token request should be sent to regional ESTS.
        /// If set, MSAL tries to auto-detect and use a regional Azure authority. This helps keep the authentication traffic inside the Azure region. 
        /// If the region cannot be determined (e.g. not running on Azure), MSALClientException is thrown with error code region_discovery_failed.
        /// This feature requires configuration at tenant level.
        /// By default the value for this variable is false.
        /// See https://aka.ms/msal-net-region-discovery for more details.
        /// </summary>
        /// <param name="useAzureRegion"><c>true</c> if the token request should be sent to regional ESTS. The default is <c>false</c>.
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        [Obsolete("This method name has been changed to a more relevant name, please use WithPreferredAzureRegion instead which also includes added features.", true)]
        public AcquireTokenForClientParameterBuilder WithAzureRegion(bool useAzureRegion)
        {
            ValidateUseOfExpirementalFeature();

            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAzureRegion, useAzureRegion);
            Parameters.AutoDetectRegion = useAzureRegion;
            return this;
        }

        /// <summary>
        /// Specifies if the token request should be sent to regional ESTS.
        /// If set, MSAL tries to auto-detect and use a regional Azure authority. This helps keep the authentication traffic inside the Azure region. 
        /// If the region cannot be determined (e.g. not running on Azure), MSALClientException is thrown with error code region_discovery_failed.
        /// This feature requires configuration at tenant level.
        /// By default the value for this variable is false.
        /// See https://aka.ms/msal-net-region-discovery for more details.
        /// </summary>
        /// <param name="useAzureRegion"><c>true</c> if the token request should be sent to regional ESTS. The default is <c>false</c>.
        /// </param>
        /// <param name="regionUsedIfAutoDetectFails"> optional parameter to provide region to MSAL. This parameter will be used along with auto detection of region.
        /// If the region is auto detected, the provided region will be compared with the detected region and used in telemetry to do analysis on correctness of the region provided.
        /// If auto region detection fails, the provided region will be used for instance metadata.</param>
        /// <param name="fallbackToGlobal"><c>true</c> to fallback to global ESTS endpoint when calls to regional ESTS fail.
        /// This will only happen when MSAL is not able to detect a region or if there is no provided region.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenForClientParameterBuilder WithPreferredAzureRegion(bool useAzureRegion = true, string regionUsedIfAutoDetectFails = "", bool fallbackToGlobal = true)
        {
            ValidateUseOfExpirementalFeature();

            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAzureRegion, useAzureRegion);
            Parameters.AutoDetectRegion = useAzureRegion;
            Parameters.RegionToUse = regionUsedIfAutoDetectFails;
            Parameters.FallbackToGlobal = fallbackToGlobal;
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
            return ApiEvent.ApiIds.AcquireTokenForClient;
        }
    }
}
