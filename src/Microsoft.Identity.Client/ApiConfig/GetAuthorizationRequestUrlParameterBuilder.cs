// Copyright (c) Microsoft Corporation. All rights reserved.
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
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    ///     NOTE:  a few of the methods in AbstractAcquireTokenParameterBuilder (e.g. account) don't make sense here.
    ///     Do we want to create a further base that contains ALL of the common methods, and then have another one including
    ///     account, etc
    ///     that are only used for AcquireToken?
    /// </summary>
    public sealed class GetAuthorizationRequestUrlParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<GetAuthorizationRequestUrlParameterBuilder>
    {
        private GetAuthorizationRequestUrlParameters Parameters { get; } = new GetAuthorizationRequestUrlParameters();

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.GetAuthorizationRequestUrl;

        internal GetAuthorizationRequestUrlParameterBuilder(IConfidentialClientApplicationExecutor confidentialClientApplicationexecutor)
            : base(confidentialClientApplicationexecutor)
        {
        }

        internal static GetAuthorizationRequestUrlParameterBuilder Create(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            IEnumerable<string> scopes)
        {
            return new GetAuthorizationRequestUrlParameterBuilder(confidentialClientApplicationExecutor).WithScopes(scopes);
        }

        /// <summary>
        /// Sets the redirect URI to add to the Authorization request URL
        /// </summary>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithRedirectUri(string redirectUri)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithRedirectUri);
            Parameters.RedirectUri = redirectUri;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="loginHint"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithLoginHint(string loginHint)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithLoginHint);
            Parameters.LoginHint = loginHint;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithAccount(IAccount account)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAccount);
            Parameters.Account = account;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="extraScopesToConsent"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithExtraScopesToConsent(IEnumerable<string> extraScopesToConsent)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithExtraScopesToConsent);
            Parameters.ExtraScopesToConsent = extraScopesToConsent;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("This is a developer BUG.  This should never get executed.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public new Task<Uri> ExecuteAsync(CancellationToken cancellationToken)
        {
            // This method is marked "public new" because it only differs in return type from the base class
            // ExecuteAsync() and we need this one to return Uri and not AuthenticationResult.

            ValidateAndCalculateApiId();
            return ConfidentialClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.GetAuthorizationRequestUrl;
        }
    }
#endif
}
