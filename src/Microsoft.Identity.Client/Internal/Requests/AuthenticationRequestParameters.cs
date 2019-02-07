// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class AuthenticationRequestParameters
    {
        public AuthenticationRequestParameters(
            IServiceBundle serviceBundle,
            Authority customAuthority,
            ITokenCacheInternal tokenCache,
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext)
        {
            Authority authorityInstance = customAuthority ?? (commonParameters.AuthorityOverride == null
                                                                  ? Authority.CreateAuthority(serviceBundle)
                                                                  : Authority.CreateAuthorityWithOverride(
                                                                      serviceBundle,
                                                                      commonParameters.AuthorityOverride));

            Authority = authorityInstance;
            ClientId = serviceBundle.Config.ClientId;
            CacheSessionManager = new CacheSessionManager(tokenCache, this);
            Scope = ScopeHelper.CreateSortedSetFromEnumerable(commonParameters.Scopes);
            RedirectUri = new Uri(serviceBundle.Config.RedirectUri);
            RequestContext = requestContext;
            ApiId = commonParameters.ApiId;
            IsBrokerEnabled = serviceBundle.Config.IsBrokerEnabled;

            // Set application wide query parameters.
            ExtraQueryParameters = serviceBundle.Config.ExtraQueryParameters ?? new Dictionary<string, string>();

            // Copy in call-specific query parameters.
            if (commonParameters.ExtraQueryParameters != null)
            {
                foreach (var kvp in commonParameters.ExtraQueryParameters)
                {
                    ExtraQueryParameters[kvp.Key] = kvp.Value;
                }
            }

            // Prefer the call-specific claims, otherwise use the app config
            Claims = commonParameters.Claims ?? serviceBundle.Config.Claims;
        }

        public ApiEvent.ApiIds ApiId { get; }

        public RequestContext RequestContext { get; }
        public Authority Authority { get; }
        public AuthorityInfo AuthorityInfo => Authority.AuthorityInfo;
        public AuthorityEndpoints Endpoints { get; set; }
        public string TenantUpdatedCanonicalAuthority { get; set; }
        public ICacheSessionManager CacheSessionManager { get; set; }
        public SortedSet<string> Scope { get; set; }
        public string ClientId { get; set; }
        public Uri RedirectUri { get; set; }
        public IDictionary<string, string> ExtraQueryParameters { get; }
        public string Claims { get; }

        internal bool IsBrokerEnabled { get; set; }

#region TODO REMOVE FROM HERE AND USE FROM SPECIFIC REQUEST PARAMETERS
        // TODO: ideally, these can come from the particular request instance and not be in RequestBase since it's not valid for all requests.

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms
        public ClientCredential ClientCredential { get; set; }
#endif

        // TODO: ideally, this can come from the particular request instance and not be in RequestBase since it's not valid for all requests.
        public bool SendX5C { get; set; }
        public string LoginHint { get; set; }
        public IAccount Account { get; set; }

        public bool IsClientCredentialRequest { get; set; }
        public bool IsRefreshTokenRequest { get; set; }
        public UserAssertion UserAssertion { get; set; }

#endregion

        public void LogParameters(ICoreLogger logger)
        {
            // Create Pii enabled string builder
            var builder = new StringBuilder(
                Environment.NewLine + "=== Request Data ===" + Environment.NewLine + "Authority Provided? - " +
                (Authority != null) + Environment.NewLine);
            builder.AppendLine("Client Id - " + ClientId);
            builder.AppendLine("Scopes - " + Scope?.AsSingleString());
            builder.AppendLine("Redirect Uri - " + RedirectUri?.OriginalString);
            builder.AppendLine("Extra Query Params Keys (space separated) - " + ExtraQueryParameters.Keys.AsSingleString());

            string messageWithPii = builder.ToString();

            // Create no Pii enabled string builder
            builder = new StringBuilder(
                Environment.NewLine + "=== Request Data ===" + Environment.NewLine + "Authority Provided? - " +
                (Authority != null) + Environment.NewLine);
            builder.AppendLine("Scopes - " + Scope?.AsSingleString());
            builder.AppendLine("Extra Query Params Keys (space separated) - " + ExtraQueryParameters.Keys.AsSingleString());
            logger.InfoPii(messageWithPii, builder.ToString());
        }

        public Dictionary<string, string> CreateRequestParametersForBroker()
        {
            Dictionary<string, string> brokerPayload = new Dictionary<string, string>();

            brokerPayload.Add(BrokerParameter.Authority, Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = EnumerableExtensions.AsSingleString(Scope);

            brokerPayload.Add(BrokerParameter.RequestScopes, scopes);
            brokerPayload.Add(BrokerParameter.ClientId, ClientId);
            brokerPayload.Add(BrokerParameter.CorrelationId, RequestContext.Logger.CorrelationId.ToString());
            brokerPayload.Add(BrokerParameter.ClientVersion, MsalIdHelper.GetMsalVersion());
            brokerPayload.Add(BrokerParameter.Force, "NO");
            brokerPayload.Add(BrokerParameter.RedirectUri, RedirectUri.AbsoluteUri);

            string extraQP = string.Join("&", ExtraQueryParameters.Select(x => x.Key + "=" + x.Value));
            brokerPayload.Add(BrokerParameter.ExtraQp, extraQP);

            brokerPayload.Add(BrokerParameter.Username, Account?.Username ?? string.Empty);
            brokerPayload.Add(BrokerParameter.ExtraOidcScopes, BrokerParameter.OidcScopesValue);

            return brokerPayload;
        }
    }
}