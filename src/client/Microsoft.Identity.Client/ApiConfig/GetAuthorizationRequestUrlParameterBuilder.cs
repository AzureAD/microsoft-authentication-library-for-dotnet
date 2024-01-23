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
    /// <summary>
    ///     NOTE:  a few of the methods in AbstractAcquireTokenParameterBuilder (e.g. account) don't make sense here.
    ///     Do we want to create a further base that contains ALL of the common methods, and then have another one including
    ///     account, etc
    ///     that are only used for AcquireToken?
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed class GetAuthorizationRequestUrlParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<GetAuthorizationRequestUrlParameterBuilder>
    {
        private GetAuthorizationRequestUrlParameters Parameters { get; } = new GetAuthorizationRequestUrlParameters();

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
            Parameters.RedirectUri = redirectUri;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="loginHint"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithLoginHint(string loginHint)
        {
            Parameters.LoginHint = loginHint;

            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithAccount(IAccount account)
        {
            Parameters.Account = account;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="extraScopesToConsent"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithExtraScopesToConsent(IEnumerable<string> extraScopesToConsent)
        {
            Parameters.ExtraScopesToConsent = extraScopesToConsent;
            return this;
        }

        /// <summary>
        /// Used to secure authorization code grant via Proof of Key for Code Exchange (PKCE).
        /// For more information, see the PKCE RCF:
        /// https://tools.ietf.org/html/rfc7636
        /// </summary>
        /// <param name="codeVerifier">MSAL.NET will generate it. </param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithPkce(out string codeVerifier)
        {
            Parameters.CodeVerifier = codeVerifier = ServiceBundle.PlatformProxy.CryptographyManager.GenerateCodeVerifier();
            return this;
        }

        /// <summary>
        /// To help with resiliency, the AAD backup authentication system operates as an AAD backup.
        /// This will provide the AAD backup authentication system with a routing hint to help improve performance during authentication.
        /// The hint created with this api will take precedence over the one created with <see cref="WithLoginHint"/>
        /// </summary>
        /// <param name="userObjectIdentifier">GUID which is unique to the user, parsed from the client_info.</param>
        /// <param name="tenantIdentifier">GUID format of the tenant ID, parsed from the client_info.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public GetAuthorizationRequestUrlParameterBuilder WithCcsRoutingHint(string userObjectIdentifier, string tenantIdentifier)
        {
            if (string.IsNullOrEmpty(userObjectIdentifier) || string.IsNullOrEmpty(tenantIdentifier))
            {
                return this;
            }

            Parameters.CcsRoutingHint = new KeyValuePair<string, string>(userObjectIdentifier, tenantIdentifier);
            return this;
        }

        /// <summary>
        /// Specifies the interactive experience for the user.
        /// </summary>
        /// <param name="prompt">Requested interactive experience. The default is <see cref="Prompt.SelectAccount"/>
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        public GetAuthorizationRequestUrlParameterBuilder WithPrompt(Prompt prompt)
        {
            Parameters.Prompt = prompt;
            return this;
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public new Task<Uri> ExecuteAsync()
        {
            // This method is marked "public new" because it only differs in return type from the base class
            // ExecuteAsync() and we need this one to return Uri and not AuthenticationResult.

            return ExecuteAsync(CancellationToken.None);
        }

        /// <inheritdoc/>
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.GetAuthorizationRequestUrl;
        }
    }
}
