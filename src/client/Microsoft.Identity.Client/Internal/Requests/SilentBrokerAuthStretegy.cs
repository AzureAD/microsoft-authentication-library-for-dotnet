using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class SilentBrokerAuthStretegy : ISilentAuthStrategy
    {
        internal AuthenticationRequestParameters _authenticationRequestParameters;
        private ICacheSessionManager CacheManager => _authenticationRequestParameters.CacheSessionManager;
        protected IServiceBundle _serviceBundle;
        private readonly AcquireTokenSilentParameters _silentParameters;
        private SilentRequest _silentRequest;
        private IBroker _broker;
        public Dictionary<string, string> BrokerPayload = new Dictionary<string, string>();
        ICoreLogger _logger;

        public SilentBrokerAuthStretegy(
            SilentRequest request,
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters silentParameters)
        {
            _authenticationRequestParameters = authenticationRequestParameters;
            _silentParameters = silentParameters;
            _serviceBundle = serviceBundle;
            _silentRequest = request;
            _broker = _serviceBundle.PlatformProxy.CreateBroker(null);
            _logger = authenticationRequestParameters.RequestContext.Logger;
        }

        public async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            MsalAccessTokenCacheItem cachedAccessTokenItem = null;

            if (!_silentParameters.ForceRefresh && !_authenticationRequestParameters.HasClaims)
            {
                cachedAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

                if (cachedAccessTokenItem != null && !cachedAccessTokenItem.NeedsRefresh())
                {
                    _logger.Info("Returning access token found in cache. RefreshOn exists ? "
                        + cachedAccessTokenItem.RefreshOn.HasValue);
                    _authenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;
                    return await CreateAuthenticationResultAsync(cachedAccessTokenItem).ConfigureAwait(false);
                }
            }
            else
            {
                _logger.Info("Skipped looking for an Access Token because ForceRefresh or Claims were set");
            }

            var response = await SendTokenRequestToBrokerAsync().ConfigureAwait(false);
            return await _silentRequest.CacheTokenResponseAndCreateAuthenticationResultAsync(response).ConfigureAwait(false);
        }

        public async Task<MsalTokenResponse> SendTokenRequestToBrokerAsync()
        {
            if (!_broker.IsBrokerInstalledAndInvokable())
            {
                throw new MsalClientException(MsalError.BrokerApplicationRequired, MsalErrorMessage.AndroidBrokerCannotBeInvoked);
            }

            _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);

            return await SendAndVerifyResponseAsync().ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> SendAndVerifyResponseAsync()
        {
            CreateRequestParametersForBroker();

            MsalTokenResponse msalTokenResponse =
                await _broker.AcquireTokenUsingBrokerAsync(BrokerPayload).ConfigureAwait(false);

            ValidateResponseFromBroker(msalTokenResponse);
            return msalTokenResponse;
        }

        internal void CreateRequestParametersForBroker()
        {
            BrokerPayload.Add(BrokerParameter.IsSilentBrokerRequest, "true");
            BrokerPayload.Add(BrokerParameter.Authority, _authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = EnumerableExtensions.AsSingleString(_authenticationRequestParameters.Scope);
            BrokerPayload.Add(BrokerParameter.Scope, scopes);
            BrokerPayload.Add(BrokerParameter.ClientId, _authenticationRequestParameters.ClientId);
            BrokerPayload.Add(BrokerParameter.CorrelationId, _logger.CorrelationId.ToString());
            BrokerPayload.Add(BrokerParameter.ClientVersion, MsalIdHelper.GetMsalVersion());
            BrokerPayload.Add(BrokerParameter.RedirectUri, _serviceBundle.Config.RedirectUri);
            string extraQP = string.Join("&", _authenticationRequestParameters.ExtraQueryParameters.Select(x => x.Key + "=" + x.Value));
            BrokerPayload.Add(BrokerParameter.ExtraQp, extraQP);
            BrokerPayload.Add(BrokerParameter.ExtraOidcScopes, BrokerParameter.OidcScopesValue);
            BrokerPayload.Add(BrokerParameter.HomeAccountId, _silentParameters.Account?.HomeAccountId?.Identifier);
            BrokerPayload.Add(BrokerParameter.LocalAccountId, _silentParameters.Account?.HomeAccountId?.ObjectId);
            BrokerPayload.Add(BrokerParameter.Username, !string.IsNullOrEmpty(_silentParameters.Account?.Username) ? _silentParameters.Account?.Username : _silentParameters.LoginHint);
#pragma warning disable CA1305 // Specify IFormatProvider
            BrokerPayload.Add(BrokerParameter.ForceRefresh, _silentParameters.ForceRefresh.ToString());
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        internal void ValidateResponseFromBroker(MsalTokenResponse msalTokenResponse)
        {
            _logger.Info(LogMessages.CheckMsalTokenResponseReturnedFromBroker);
            if (msalTokenResponse.AccessToken != null)
            {
                _logger.Info("Success. Response contains an access token");
                return;
            }

            if (msalTokenResponse.Error != null)
            {
                _logger.Info(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));
                throw new MsalServiceException(msalTokenResponse.Error, MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }

            _logger.Info(LogMessages.UnknownErrorReturnedInBrokerResponse);
            throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);
        }

        public Task PreRunAsync()
        {
            return null;
        }

        private async Task<AuthenticationResult> CreateAuthenticationResultAsync(MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            var msalIdTokenItem = await CacheManager.GetIdTokenCacheItemAsync(cachedAccessTokenItem.GetIdTokenItemKey()).ConfigureAwait(false);
            return new AuthenticationResult(
                cachedAccessTokenItem,
                msalIdTokenItem,
                _authenticationRequestParameters.AuthenticationScheme,
                _authenticationRequestParameters.RequestContext.CorrelationId);
        }
    }
}
