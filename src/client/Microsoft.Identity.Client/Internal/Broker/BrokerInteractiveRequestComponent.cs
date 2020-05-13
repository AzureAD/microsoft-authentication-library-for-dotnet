// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Broker
{

    // TODO: bogavril - there is really no need for anything in this class to be public (including Broker and BrokerPayload)
    // except for the ctor and ExecuteAsync. Everything else is testable by mocking IBroker.
    internal class BrokerInteractiveRequestComponent : ITokenRequestComponent
    {
        internal Dictionary<string, string> BrokerPayload { get; set; } = new Dictionary<string, string>();
        internal IBroker Broker { get; }
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;
        private readonly string _optionalBrokerInstallUrl; // can be null
        private readonly AuthenticationRequestParameters _authenticationRequestParameters;
        private readonly IServiceBundle _serviceBundle;

        public BrokerInteractiveRequestComponent(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters,
            IBroker broker,
            string optionalBrokerInstallUrl)
        {
            _authenticationRequestParameters = authenticationRequestParameters;
            _interactiveParameters = acquireTokenInteractiveParameters;
            _serviceBundle = authenticationRequestParameters.RequestContext.ServiceBundle;
            Broker = broker;
            _optionalBrokerInstallUrl = optionalBrokerInstallUrl;
        }

        public async Task<MsalTokenResponse> FetchTokensAsync(CancellationToken cancellationToken)
        {
            if (Broker.IsBrokerInstalledAndInvokable())
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);
            }
            else
            {
                if (string.IsNullOrEmpty(_optionalBrokerInstallUrl))
                {
                    _authenticationRequestParameters.RequestContext.Logger.Info("Broker is required but not installed. An app uri has not been provided.");
                    return null;
                }

                _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.AddBrokerInstallUrlToPayload);
                BrokerPayload[BrokerParameter.BrokerInstallUrl] = _optionalBrokerInstallUrl;
            }

            var tokenResponse = await SendAndVerifyResponseAsync().ConfigureAwait(false);
            return tokenResponse;
        }

        private async Task<MsalTokenResponse> SendAndVerifyResponseAsync()
        {
            CreateRequestParametersForBroker();

            MsalTokenResponse msalTokenResponse =
                await Broker.AcquireTokenUsingBrokerAsync(BrokerPayload).ConfigureAwait(false);

            ValidateResponseFromBroker(msalTokenResponse);
            return msalTokenResponse;
        }


        internal /* internal for test */ void CreateRequestParametersForBroker()
        {
            BrokerPayload.Clear();
            BrokerPayload.Add(BrokerParameter.Authority, _authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = EnumerableExtensions.AsSingleString(_authenticationRequestParameters.Scope);

            BrokerPayload.Add(BrokerParameter.Scope, scopes);
            BrokerPayload.Add(BrokerParameter.ClientId, _authenticationRequestParameters.ClientId);
            BrokerPayload.Add(BrokerParameter.CorrelationId, _authenticationRequestParameters.RequestContext.Logger.CorrelationId.ToString());
            BrokerPayload.Add(BrokerParameter.ClientVersion, MsalIdHelper.GetMsalVersion());
            BrokerPayload.Add(BrokerParameter.Force, "NO");
            BrokerPayload.Add(BrokerParameter.RedirectUri, _serviceBundle.Config.RedirectUri);

            string extraQP = string.Join("&", _authenticationRequestParameters.ExtraQueryParameters.Select(x => x.Key + "=" + x.Value));
            BrokerPayload.Add(BrokerParameter.ExtraQp, extraQP);

            BrokerPayload.Add(BrokerParameter.Username, _authenticationRequestParameters.Account?.Username ?? string.Empty);
            BrokerPayload.Add(BrokerParameter.ExtraOidcScopes, BrokerParameter.OidcScopesValue);
            BrokerPayload.Add(BrokerParameter.Prompt, _interactiveParameters.Prompt.PromptValue);
        }

        internal /* internal for test */ void ValidateResponseFromBroker(MsalTokenResponse msalTokenResponse)
        {
            _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CheckMsalTokenResponseReturnedFromBroker);
            if (msalTokenResponse.AccessToken != null)
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(
                    LogMessages.BrokerResponseContainsAccessToken +
                    msalTokenResponse.AccessToken.Count());
                return;
            }
            else if (msalTokenResponse.Error != null)
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));
                throw new MsalServiceException(msalTokenResponse.Error, MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }
            else
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.UnknownErrorReturnedInBrokerResponse);
                throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);
            }
        }

        // Example auth code that shows that broker is required:
        // msauth://wpj?username=joe@contoso.onmicrosoft.com&app_link=itms%3a%2f%2fitunes.apple.com%2fapp%2fazure-authenticator%2fid983156458%3fmt%3d8
        public static bool IsBrokerRequiredAuthCode(string authCode, out string installationUri)
        {
            if (authCode.StartsWith(BrokerParameter.AuthCodePrefixForEmbeddedWebviewBrokerInstallRequired, StringComparison.OrdinalIgnoreCase))
            //|| authCode.StartsWith(_serviceBundle.Config.RedirectUri, StringComparison.OrdinalIgnoreCase) // TODO: what is this?!
            {
                installationUri = ExtractAppLink(authCode);
                return (installationUri != null);
            }

            installationUri = null;
            return false;
        }

        private static string ExtractAppLink(string authCode)
        {
            Uri authCodeUri = new Uri(authCode);
            string query = authCodeUri.Query;

            if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Substring(1);
            }

            Dictionary<string, string> queryDict = CoreHelpers.ParseKeyValueList(query, '&', true, true, null);

            if (!queryDict.ContainsKey(BrokerParameter.AppLink))
            {
                return null;
            }

            return queryDict[BrokerParameter.AppLink];
        }
    }
}
