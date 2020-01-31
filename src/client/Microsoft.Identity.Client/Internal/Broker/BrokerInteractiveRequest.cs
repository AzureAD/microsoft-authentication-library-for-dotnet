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
    internal class BrokerInteractiveRequest : InteractiveRequest
    {
        internal Dictionary<string, string> BrokerPayload { get; set; } = new Dictionary<string, string>();
        internal IBroker Broker { get; }
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;

        internal BrokerInteractiveRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters,
            IWebUI webUI,
            IBroker broker)
            : base(
                  serviceBundle,
                  authenticationRequestParameters,
                  acquireTokenInteractiveParameters,
                  webUI)
        {
            _interactiveParameters = acquireTokenInteractiveParameters;
            Broker = broker;
        }

        internal async Task<MsalTokenResponse> ExecuteBrokerAsync(CancellationToken cancellationToken)
        {
            return await SendTokenRequestToBrokerAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<MsalTokenResponse> SendTokenRequestToBrokerAsync(CancellationToken cancellationToken)
        {
            if (Broker.CanInvokeBroker())
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);
                BrokerPayload.Clear();
                return await SendAndVerifyResponseAsync().ConfigureAwait(false);
            }
            else
            {
                if (!string.IsNullOrEmpty(AuthResult?.Code)) // The user has already signed in and auth code contains msauth
                {
                    AuthenticationRequestParameters.RequestContext.Logger.Info(LogMessages.UserPreviouslySignedInBrokerRequired);
                    await BrokerRequiredToAcquireTokenAsync().ConfigureAwait(false);
                }
                else
                {
                    AuthenticationRequestParameters.RequestContext.Logger.Info("The developer set .WithBroker(true), but broker is not installed" +
                        "on the device. Will take the user through the sign-in experience to generate the auth code...");
                    await ExecuteAuthorizationAsync(cancellationToken).ConfigureAwait(false); // The user goes through the sign-in process

                    if (IsBrokerInvocationRequired()) // If auth code is prefixed w/msauth, broker is required due to conditional access policies
                    {
                        await BrokerRequiredToAcquireTokenAsync().ConfigureAwait(false);
                    }
                }

                // broker not needed based on auth code result. Continue w/regular acquire token call
                return await SendTokenRequestAsync(GetBodyParameters(), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<MsalTokenResponse> BrokerRequiredToAcquireTokenAsync()
        {
            BrokerPayload.Clear();
            AuthenticationRequestParameters.RequestContext.Logger.Info(LogMessages.AddBrokerInstallUrlToPayload);
            BrokerPayload[BrokerParameter.BrokerInstallUrl] = AuthResult.Code;
            return await SendAndVerifyResponseAsync().ConfigureAwait(false);
        }

        internal override async Task ExecuteAuthorizationAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);
            await AcquireAuthorizationAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(AuthResult.Code)) // something failed, make sure error message is returned
            {
                VerifyAuthorizationResult();
            }
        }

        private async Task<MsalTokenResponse> SendAndVerifyResponseAsync()
        {
            CreateRequestParametersForBroker();

            MsalTokenResponse msalTokenResponse =
                await Broker.AcquireTokenUsingBrokerAsync(BrokerPayload).ConfigureAwait(false);

            ValidateResponseFromBroker(msalTokenResponse);
            return msalTokenResponse;
        }

        internal void CreateRequestParametersForBroker()
        {
            BrokerPayload.Add(BrokerParameter.Authority, AuthenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = EnumerableExtensions.AsSingleString(AuthenticationRequestParameters.Scope);

            BrokerPayload.Add(BrokerParameter.Scope, scopes);
            BrokerPayload.Add(BrokerParameter.ClientId, AuthenticationRequestParameters.ClientId);
            BrokerPayload.Add(BrokerParameter.CorrelationId, AuthenticationRequestParameters.RequestContext.Logger.CorrelationId.ToString());
            BrokerPayload.Add(BrokerParameter.ClientVersion, MsalIdHelper.GetMsalVersion());
            BrokerPayload.Add(BrokerParameter.Force, "NO");
            BrokerPayload.Add(BrokerParameter.RedirectUri, ServiceBundle.Config.RedirectUri);

            string extraQP = string.Join("&", AuthenticationRequestParameters.ExtraQueryParameters.Select(x => x.Key + "=" + x.Value));
            BrokerPayload.Add(BrokerParameter.ExtraQp, extraQP);

            BrokerPayload.Add(BrokerParameter.Username, AuthenticationRequestParameters.Account?.Username ?? string.Empty);
            BrokerPayload.Add(BrokerParameter.ExtraOidcScopes, BrokerParameter.OidcScopesValue);
            BrokerPayload.Add(BrokerParameter.Prompt, _interactiveParameters.Prompt.PromptValue);
        }

        internal void ValidateResponseFromBroker(MsalTokenResponse msalTokenResponse)
        {
            AuthenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CheckMsalTokenResponseReturnedFromBroker);
            if (msalTokenResponse.AccessToken != null)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    LogMessages.BrokerResponseContainsAccessToken +
                    msalTokenResponse.AccessToken.Count());
                return;
            }
            else if (msalTokenResponse.Error != null)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));
                throw new MsalServiceException(msalTokenResponse.Error, MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }
            else
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(LogMessages.UnknownErrorReturnedInBrokerResponse);
                throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);
            }
        }
    }
}
