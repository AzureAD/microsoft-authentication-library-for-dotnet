// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
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
            string homeAccountId = null)
        {
            _serviceBundle = serviceBundle;
            _commonParameters = commonParameters;

            Authority = Authority.CreateAuthorityForRequest(serviceBundle.Config.AuthorityInfo, commonParameters.AuthorityOverride);

            ClientId = serviceBundle.Config.ClientId;
            CacheSessionManager = new CacheSessionManager(tokenCache, this);
            Scope = ScopeHelper.CreateScopeSet(commonParameters.Scopes);
            RedirectUri = new Uri(serviceBundle.Config.RedirectUri);
            RequestContext = requestContext;
            IsBrokerConfigured = serviceBundle.Config.IsBrokerEnabled;

            // Set application wide query parameters.
            ExtraQueryParameters = serviceBundle.Config.ExtraQueryParameters ?? new Dictionary<string, string>();

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

        public ApiTelemetryId ApiTelemId => _commonParameters.ApiTelemId;

        public IEnumerable<KeyValuePair<string, string>> GetApiTelemetryFeatures()
        {
            return _commonParameters.GetApiTelemetryFeatures();
        }

        public ApiEvent.ApiIds ApiId => _commonParameters.ApiId;

        public RequestContext RequestContext { get; }
        public Authority Authority { get; set; }
        public AuthorityInfo AuthorityInfo => Authority.AuthorityInfo;
        public AuthorityEndpoints Endpoints { get; set; }
        public Authority TenantUpdatedCanonicalAuthority { get; set; }
        public ICacheSessionManager CacheSessionManager { get; }
        public HashSet<string> Scope { get; }

        public bool HasScopes => Scope != null && Scope.Any();

        public string ClientId { get; }

        public Uri RedirectUri { get; set; }
       
        public IDictionary<string, string> ExtraQueryParameters { get; }

        public string ClaimsAndClientCapabilities { get; private set; }    

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

        public AuthorityInfo AuthorityOverride => _commonParameters.AuthorityOverride;

        internal bool IsBrokerConfigured { get; set; /* set only for test */ }

        public IAuthenticationScheme AuthenticationScheme => _commonParameters.AuthenticationScheme;

        #region TODO REMOVE FROM HERE AND USE FROM SPECIFIC REQUEST PARAMETERS
        // TODO: ideally, these can come from the particular request instance and not be in RequestBase since it's not valid for all requests.

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

        public ClientCredentialWrapper ClientCredential { get; set; }
#endif
        // TODO: ideally, this can come from the particular request instance and not be in RequestBase since it's not valid for all requests.
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

        public string HomeAccountId { get;}


        public bool IsClientCredentialRequest => ApiId == ApiEvent.ApiIds.AcquireTokenForClient;
        public bool IsConfidentialClient
        {
            get
            {
#if ANDROID || iOS || WINDOWS_APP || MAC
                return false;
#else
                return ClientCredential != null;
#endif
            }
        }
        public UserAssertion UserAssertion { get; set; }

#endregion

        public void LogParameters()
        {
            var logger = this.RequestContext.Logger;

            // Create Pii enabled string builder
            var builder = new StringBuilder(
                Environment.NewLine + "=== Request Data ===" + Environment.NewLine + "Authority Provided? - " +
                (Authority != null) + Environment.NewLine);
            builder.AppendLine("Client Id - " + ClientId);
            builder.AppendLine("Scopes - " + Scope?.AsSingleString());
            builder.AppendLine("Redirect Uri - " + RedirectUri?.OriginalString);
            builder.AppendLine("Extra Query Params Keys (space separated) - " + ExtraQueryParameters.Keys.AsSingleString());
            builder.AppendLine("ClaimsAndClientCapabilities - " + ClaimsAndClientCapabilities);
            builder.AppendLine("Authority - " + AuthorityInfo?.CanonicalAuthority);
            builder.AppendLine("ApiId - " + ApiId);
            builder.AppendLine("IsConfidentialClient - " + IsConfidentialClient);
            builder.AppendLine("SendX5C - " + SendX5C);
            builder.AppendLine("LoginHint - " + LoginHint);
            builder.AppendLine("IsBrokerConfigured - " + IsBrokerConfigured);
            builder.AppendLine("HomeAccountId - " + HomeAccountId);

            string messageWithPii = builder.ToString();

            // Create no Pii enabled string builder
            builder = new StringBuilder(
                Environment.NewLine + "=== Request Data ===" +
                Environment.NewLine + "Authority Provided? - " + (Authority != null) +
                Environment.NewLine);
            builder.AppendLine("Scopes - " + Scope?.AsSingleString());
            builder.AppendLine("Extra Query Params Keys (space separated) - " + ExtraQueryParameters.Keys.AsSingleString());
            builder.AppendLine("ApiId - " + ApiId);
            builder.AppendLine("IsConfidentialClient - " + IsConfidentialClient);
            builder.AppendLine("SendX5C - " + SendX5C);
            builder.AppendLine("LoginHint ? " + !string.IsNullOrEmpty(LoginHint));
            builder.AppendLine("IsBrokerConfigured - " + IsBrokerConfigured);
            builder.AppendLine("HomeAccountId - " + !string.IsNullOrEmpty(HomeAccountId));


            logger.InfoPii(messageWithPii, builder.ToString());
        }

    }
}
