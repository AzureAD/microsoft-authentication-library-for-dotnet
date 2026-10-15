// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.Platforms.net;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    internal class BrokerRequest : BrokerRequestMetadata
    {
        private Uri _redirectUri;
        public Uri RedirectUri
        {
            get => _redirectUri;
            set
            {
                _redirectUri = value;
                UrlEncodedRedirectUri = GetEncodedRedirectUri(value);
            }
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
            br.Authority = authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority.ToString();
            br.Scopes = EnumerableExtensions.AsSingleString(authenticationRequestParameters.Scope);
            br.ClientId = authenticationRequestParameters.AppConfig.ClientId;
            br.CorrelationId = authenticationRequestParameters.RequestContext.CorrelationId.ToString();
#pragma warning disable CS0618  
            br.ClientAppVersion = Application.Context.PackageManager.GetPackageInfo(
                Application.Context.PackageName,
                PackageInfoFlags.MatchAll).VersionName;
#pragma warning restore CS0618  
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
