// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text.Json.Serialization;
using Android.App;
using Android.Content.PM;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    [Preserve(AllMembers = true)]
    internal class BrokerRequest
    {
        [JsonPropertyName("authority")]
        public string Authority { get; set; }
        [JsonPropertyName("scopes")]
        public string Scopes { get; set; }
        [JsonPropertyName("redirect_uri")]
        public string UrlEncodedRedirectUri
        {
            get { return GetEncodedRedirectUri(RedirectUri); }
        }

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }
        [JsonPropertyName("home_account_id")]
        public string HomeAccountId { get; set; }
        [JsonPropertyName("local_account_id")]
        public string LocalAccountId { get; set; }
        [JsonPropertyName("username")]
        public string UserName { get; set; }
        [JsonPropertyName("extra_query_param")]
        public string ExtraQueryParameters { get; set; }
        [JsonPropertyName("correlation_id")]
        public string CorrelationId { get; set; }
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }
        [JsonPropertyName("claims")]
        public string Claims { get; set; }
        [JsonPropertyName("force_refresh")]
        public string ForceRefresh { get; set; }
        [JsonPropertyName("client_app_name")]
        public string ClientAppName { get; set; }
        [JsonPropertyName("client_app_version")]
        public string ClientAppVersion { get; set; }
        [JsonPropertyName("client_version")]
        public string ClientVersion { get; set; }
        [JsonPropertyName("environment")]
        public string Environment { get; set; }

        [JsonIgnore]
        public Uri RedirectUri
        {
            get; set;
        }

        public static BrokerRequest FromInteractiveParameters(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            BrokerRequest br = FromAuthenticationParameters(authenticationRequestParameters);
            var prompt = acquireTokenInteractiveParameters.Prompt;

            if (prompt == Client.Prompt.NoPrompt || prompt == Client.Prompt.NotSpecified)
            {
                br.Prompt = Client.Prompt.SelectAccount.PromptValue.ToUpperInvariant();
            }
            else
            {
                br.Prompt = prompt.PromptValue.ToUpperInvariant();
            }

            br.UserName = acquireTokenInteractiveParameters.LoginHint;

            return br;
        }

        public static BrokerRequest FromSilentParameters(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            BrokerRequest br = FromAuthenticationParameters(authenticationRequestParameters);

#pragma warning disable CA1305 // Specify IFormatProvider
            br.ForceRefresh = acquireTokenSilentParameters.ForceRefresh.ToString();
#pragma warning restore CA1305 // Specify IFormatProvider

            br.UserName = !string.IsNullOrEmpty(acquireTokenSilentParameters.Account?.Username) ?
                acquireTokenSilentParameters.Account?.Username :
                acquireTokenSilentParameters.LoginHint;

            br.HomeAccountId = acquireTokenSilentParameters.Account?.HomeAccountId?.Identifier;
            br.LocalAccountId = acquireTokenSilentParameters.Account?.HomeAccountId?.ObjectId;

            return br;
        }

        private static BrokerRequest FromAuthenticationParameters(AuthenticationRequestParameters authenticationRequestParameters)
        {
            BrokerRequest br = new BrokerRequest();
            br.Authority = authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority;
            br.Scopes = EnumerableExtensions.AsSingleString(authenticationRequestParameters.Scope);
            br.ClientId = authenticationRequestParameters.AppConfig.ClientId;
            br.CorrelationId = authenticationRequestParameters.RequestContext.CorrelationId.ToString();
            
            br.ClientAppVersion = Application.Context.PackageManager.GetPackageInfo(
                Application.Context.PackageName,
                PackageInfoFlags.MatchAll).VersionName;
            br.ClientAppName = Application.Context.PackageName;
            br.ClientVersion = MsalIdHelper.GetMsalVersion();

            br.RedirectUri = authenticationRequestParameters.RedirectUri;

            if (authenticationRequestParameters.RequestContext.ServiceBundle.Config.ClientCapabilities?.Any() == true)
            {
                br.Claims = authenticationRequestParameters.ClaimsAndClientCapabilities;
            }

            if (authenticationRequestParameters.ExtraQueryParameters?.Any() == true)
            {
                string extraQP = string.Join("&", authenticationRequestParameters.ExtraQueryParameters.Select(x => x.Key + "=" + x.Value));
                br.ExtraQueryParameters = extraQP;
            }

            return br;
        }

        private static string GetEncodedRedirectUri(Uri uri)
        {
            return "msauth://" + uri.Host + "/" + System.Net.WebUtility.UrlEncode(uri.AbsolutePath.Substring(1));
        }
    }
}
