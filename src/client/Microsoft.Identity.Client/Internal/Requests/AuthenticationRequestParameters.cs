// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.Extensibility.AbstractConfidentialClientAcquireTokenParameterBuilderExtension;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// This class is responsible for merging app level and request level parameters. 
    /// Not all parameters need to be merged - app level parameters can be accessed via AppConfig property
    /// </summary>
    internal class AuthenticationRequestParameters
    {
        private readonly IServiceBundle _serviceBundle;
        private readonly AcquireTokenCommonParameters _commonParameters;
        private string _loginHint;

        public AuthenticationRequestParameters(
            IServiceBundle serviceBundle,
            ITokenCacheInternal tokenCache,
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext,
            Authority initialAuthority,
            string homeAccountId = null)
        {
            _serviceBundle = serviceBundle;
            _commonParameters = commonParameters;
            RequestContext = requestContext;

            CacheSessionManager = new CacheSessionManager(tokenCache, this);
            Scope = ScopeHelper.CreateScopeSet(commonParameters.Scopes);
            RedirectUri = new Uri(serviceBundle.Config.RedirectUri);
            AuthorityManager = new AuthorityManager(RequestContext, initialAuthority);

            // Set application wide query parameters.
            ExtraQueryParameters = serviceBundle.Config.ExtraQueryParameters ??
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Copy in call-specific query parameters.
            if (commonParameters.ExtraQueryParameters != null)
            {
                foreach (KeyValuePair<string, string> kvp in commonParameters.ExtraQueryParameters)
                {
                    ExtraQueryParameters[kvp.Key] = kvp.Value;
                }
            }

            ClaimsAndClientCapabilities = ClaimsHelper.GetMergedClaimsAndClientCapabilities(
                _commonParameters.Claims,
                _serviceBundle.Config.ClientCapabilities);

            HomeAccountId = homeAccountId;
        }

        public ApplicationConfiguration AppConfig => _serviceBundle.Config;

        public ApiEvent.ApiIds ApiId => _commonParameters.ApiId;

        public RequestContext RequestContext { get; }

        #region Authority

        public AuthorityManager AuthorityManager { get; set; }

        /// <summary>
        /// Authority is the URI used by MSAL for communication and storage
        /// During a request it can be updated: 
        /// - with the preferred environment
        /// - with actual tenant
        /// </summary>
        public Authority Authority => AuthorityManager.Authority;

        public AuthorityInfo AuthorityInfo => AuthorityManager.Authority.AuthorityInfo;

        public AuthorityInfo AuthorityOverride => _commonParameters.AuthorityOverride;

        /// <summary>
        /// Gets the token endpoint of the "current" authority, as tracked by the AuthorityManager
        /// </summary>
        /// <returns></returns>
        public Task<string> GetTokenEndpointAsync(RequestContext requestContext)
        {
            return AuthorityManager.Authority.GetTokenEndpointAsync(
                requestContext);
        }

        #endregion

        public ICacheSessionManager CacheSessionManager { get; }
        public HashSet<string> Scope { get; }
        public Uri RedirectUri { get; set; }

        public IDictionary<string, string> ExtraQueryParameters { get; }

        public string ClaimsAndClientCapabilities { get; private set; }

        public Guid CorrelationId => _commonParameters.CorrelationId;

        /// <summary>
        /// Indicates if the user configured claims via .WithClaims. Not affected by Client Capabilities
        /// </summary>
        /// <remarks>If user configured claims, request should bypass cache</remarks>
        public string Claims
        {
            get
            {
                return _commonParameters.Claims;
            }
        }

        public IAuthenticationScheme AuthenticationScheme => _commonParameters.AuthenticationScheme;

        #region TODO REMOVE FROM HERE AND USE FROM SPECIFIC REQUEST PARAMETERS
        // TODO: ideally, these can come from the particular request instance and not be in RequestBase since it's not valid for all requests.

        // This should be set on a per-application basis, but can be overridden on a per-request basis should it be needed. 
        public bool SendX5C { get; set; }

        public string LoginHint
        {
            get
            {
                if (string.IsNullOrEmpty(_loginHint) && Account != null)
                {
                    return Account.Username;
                }

                return _loginHint;
            }

            set => _loginHint = value;
        }
        public IAccount Account { get; set; }

        public string HomeAccountId { get; }

        /// <summary>
        /// If set, MSAL should add the key / value pairs from the provider to the token endpoint instead of generating a client assertion
        /// </summary>
        public Func<OnBeforeTokenRequestData, Task> OnBeforeTokenRequestHandler { get => _commonParameters.OnBeforeTokenRequestHandler; }

        public IDictionary<string, string> ExtraHttpHeaders => _commonParameters.ExtraHttpHeaders;

        public bool IsClientCredentialRequest => ApiId == ApiEvent.ApiIds.AcquireTokenForClient;

        public PoPAuthenticationConfiguration PopAuthenticationConfiguration => _commonParameters.PopAuthenticationConfiguration;

        /// <remarks>
        /// User assertion is null when <see cref="ILongRunningWebApi.AcquireTokenInLongRunningProcess"/> is called.
        /// </remarks>
        public UserAssertion UserAssertion { get; set; }

        /// <summary>
        /// User-provided cache key for long-running OBO flow.
        /// </summary>
        public string LongRunningOboCacheKey { get; set; }

        public KeyValuePair<string, string>? CcsRoutingHint { get; set; }
        #endregion

        public void LogParameters()
        {
            var logger = RequestContext.Logger;

            if (logger.IsLoggingEnabled(LogLevel.Info))
            {

                // Create PII enabled string builder
                var builder = new StringBuilder(
                    Environment.NewLine + "=== Request Data ===" + Environment.NewLine + "Authority Provided? - " +
                    (Authority != null) + Environment.NewLine);
                builder.AppendLine("Client Id - " + AppConfig.ClientId);
                builder.AppendLine("Scopes - " + Scope?.AsSingleString());
                builder.AppendLine("Redirect Uri - " + RedirectUri?.OriginalString);
                builder.AppendLine("Extra Query Params Keys (space separated) - " + ExtraQueryParameters.Keys.AsSingleString());
                builder.AppendLine("ClaimsAndClientCapabilities - " + ClaimsAndClientCapabilities);
                builder.AppendLine("Authority - " + AuthorityInfo?.CanonicalAuthority);
                builder.AppendLine("ApiId - " + ApiId);
                builder.AppendLine("IsConfidentialClient - " + AppConfig.IsConfidentialClient);
                builder.AppendLine("SendX5C - " + SendX5C);
                builder.AppendLine("LoginHint - " + LoginHint);
                builder.AppendLine("IsBrokerConfigured - " + AppConfig.IsBrokerEnabled);
                builder.AppendLine("HomeAccountId - " + HomeAccountId);
                builder.AppendLine("CorrelationId - " + CorrelationId);
                builder.AppendLine("UserAssertion set: " + (UserAssertion != null));
                builder.AppendLine("LongRunningOboCacheKey set: " + !string.IsNullOrWhiteSpace(LongRunningOboCacheKey));
                builder.AppendLine("Region configured: " + AppConfig.AzureRegion);

                string messageWithPii = builder.ToString();

                // Create no PII enabled string builder
                builder = new StringBuilder(
                    Environment.NewLine + "=== Request Data ===" +
                    Environment.NewLine + "Authority Provided? - " + (Authority != null) +
                    Environment.NewLine);
                builder.AppendLine("Scopes - " + Scope?.AsSingleString());
                builder.AppendLine("Extra Query Params Keys (space separated) - " + ExtraQueryParameters.Keys.AsSingleString());
                builder.AppendLine("ApiId - " + ApiId);
                builder.AppendLine("IsConfidentialClient - " + AppConfig.IsConfidentialClient);
                builder.AppendLine("SendX5C - " + SendX5C);
                builder.AppendLine("LoginHint ? " + !string.IsNullOrEmpty(LoginHint));
                builder.AppendLine("IsBrokerConfigured - " + AppConfig.IsBrokerEnabled);
                builder.AppendLine("HomeAccountId - " + !string.IsNullOrEmpty(HomeAccountId));
                builder.AppendLine("CorrelationId - " + CorrelationId);
                builder.AppendLine("UserAssertion set: " + (UserAssertion != null));
                builder.AppendLine("LongRunningOboCacheKey set: " + !string.IsNullOrWhiteSpace(LongRunningOboCacheKey));
                builder.AppendLine("Region configured: " + AppConfig.AzureRegion);

                logger.InfoPii(messageWithPii, builder.ToString());
            }
        }
    }
}
