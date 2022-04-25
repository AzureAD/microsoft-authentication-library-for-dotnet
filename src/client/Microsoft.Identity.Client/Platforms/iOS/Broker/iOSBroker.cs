// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;
using UIKit;
using Foundation;
using System;
using CoreFoundation;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using System.Globalization;
using Security;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using System.Linq;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance.Discovery;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    /// <summary>
    /// Handles requests which invoke the broker. This is only for mobile (iOS and Android) scenarios.
    /// </summary>
    internal class iOSBroker : NSObject, IBroker
    {
        private static SemaphoreSlim s_brokerResponseReady = null;
        private static NSUrl s_brokerResponse = null;

        private readonly ICoreLogger _logger;
        private readonly ICryptographyManager _cryptoManager;
        private readonly CoreUIParent _uIParent;
        private string _brokerRequestNonce;
        private bool _brokerV3Installed = false;

        public iOSBroker(ICoreLogger logger, ICryptographyManager cryptoManager, CoreUIParent uIParent)
        {
            _logger = logger;
            _cryptoManager = cryptoManager;
            _uIParent = uIParent;
        }

        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
            using (_logger.LogMethodDuration())
            {
                if (_uIParent?.CallerViewController == null)
                {
                    _logger.Error(iOSBrokerConstants.CallerViewControllerIsNullCannotInvokeBroker);
                    throw new MsalClientException(MsalError.UIViewControllerRequiredForiOSBroker, MsalErrorMessage.UIViewControllerIsRequiredToInvokeiOSBroker);
                }

                bool canStartBroker = false;

                _uIParent.CallerViewController.InvokeOnMainThread(() =>
                {
                    if (IsBrokerInstalled(BrokerParameter.UriSchemeBrokerV3))
                    {
                        _logger.Info(iOSBrokerConstants.iOSBrokerv3Installed);
                        _brokerV3Installed = true;
                        canStartBroker = true;
                    }
                });

                if (!canStartBroker)
                {
                    _uIParent.CallerViewController.InvokeOnMainThread(() =>
                    {
                        if (IsBrokerInstalled(BrokerParameter.UriSchemeBrokerV2))
                        {
                            _logger.Info(iOSBrokerConstants.iOSBrokerv2Installed);
                            canStartBroker = true;
                        }
                    });
                }

                if (!canStartBroker)
                {
                    _logger.Info(iOSBrokerConstants.CanInvokeBrokerReturnsFalseMessage);
                }

                return canStartBroker;
            }
        }

        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            ValidateRedirectUri(authenticationRequestParameters.RedirectUri);
            AuthenticationContinuationHelper.LastRequestLogger = _logger;

            using (_logger.LogMethodDuration())
            {
                Dictionary<string, string> brokerRequest = CreateBrokerRequestDictionary(
                    authenticationRequestParameters,
                    acquireTokenInteractiveParameters);

                await InvokeIosBrokerAsync(brokerRequest).ConfigureAwait(false);

                return ProcessBrokerResponse();
            }
        }

        private void ValidateRedirectUri(Uri redirectUri)
        {
            string bundleId = NSBundle.MainBundle.BundleIdentifier;
            RedirectUriHelper.ValidateIosBrokerRedirectUri(redirectUri, bundleId, _logger);
        }

        private Dictionary<string, string> CreateBrokerRequestDictionary(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            var brokerRequest = new Dictionary<string, string>(16);

            brokerRequest.Add(BrokerParameter.Authority, authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = EnumerableExtensions.AsSingleString(authenticationRequestParameters.Scope);
            brokerRequest.Add(BrokerParameter.Scope, scopes);
            brokerRequest.Add(BrokerParameter.ClientId, authenticationRequestParameters.AppConfig.ClientId);
            brokerRequest.Add(BrokerParameter.CorrelationId, authenticationRequestParameters.RequestContext.CorrelationId.ToString());
            brokerRequest.Add(BrokerParameter.ClientVersion, MsalIdHelper.GetMsalVersion());

            // this needs to be case sensitive because the AppBundle is case sensitive
            brokerRequest.Add(
                BrokerParameter.RedirectUri, 
                authenticationRequestParameters.RedirectUri.OriginalString);

            if (authenticationRequestParameters.ExtraQueryParameters?.Any() == true)
            {
                string extraQP = string.Join("&", authenticationRequestParameters.ExtraQueryParameters.Select(x => x.Key + "=" + x.Value));
                brokerRequest.Add(BrokerParameter.ExtraQp, extraQP);
            }

            if (authenticationRequestParameters.RequestContext.ServiceBundle.Config.ClientCapabilities?.Any() == true)
            {
                var capabilities = String.Join(',', authenticationRequestParameters.RequestContext.ServiceBundle.Config.ClientCapabilities);
                brokerRequest.Add(BrokerParameter.ClientCapabilities, capabilities);
            }

            brokerRequest.Add(BrokerParameter.Username, authenticationRequestParameters.Account?.Username ?? string.Empty);
            brokerRequest.Add(BrokerParameter.ExtraOidcScopes, BrokerParameter.OidcScopesValue);

            var prompt = acquireTokenInteractiveParameters.Prompt;

            if (prompt == Prompt.NoPrompt || prompt == Prompt.NotSpecified)
            {
                brokerRequest.Add(BrokerParameter.Prompt, Prompt.SelectAccount.PromptValue);
            }
            else
            {
                brokerRequest.Add(BrokerParameter.Prompt, acquireTokenInteractiveParameters.Prompt.PromptValue);
            }
            
            if (!string.IsNullOrEmpty(authenticationRequestParameters.Claims))
            {
                brokerRequest.Add(BrokerParameter.Claims, authenticationRequestParameters.Claims);
            }

            AddCommunicationParams(brokerRequest);

            return brokerRequest;
        }

        public void HandleInstallUrl(string appLink)
        {
            DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(new NSUrl(appLink)));

            throw new MsalClientException(
                MsalError.BrokerApplicationRequired,
                MsalErrorMessage.BrokerApplicationRequired);
        }

        private void AddCommunicationParams(Dictionary<string, string> brokerRequest)
        {
            string encodedBrokerKey = Base64UrlHelpers.Encode(BrokerKeyHelper.GetOrCreateBrokerKey(_logger));
            brokerRequest[iOSBrokerConstants.BrokerKey] = encodedBrokerKey;
            brokerRequest[iOSBrokerConstants.MsgProtocolVer] = BrokerParameter.MsgProtocolVersion3;

            if (_brokerV3Installed)
            {
                _brokerRequestNonce = Guid.NewGuid().ToString();
                brokerRequest[iOSBrokerConstants.BrokerNonce] = _brokerRequestNonce;

                string applicationToken = TryReadBrokerApplicationTokenFromKeychain(brokerRequest);

                if (!string.IsNullOrEmpty(applicationToken))
                {
                    brokerRequest[iOSBrokerConstants.ApplicationToken] = applicationToken;
                }
            }
        }

        private async Task InvokeIosBrokerAsync(Dictionary<string, string> brokerPayload)
        {
            s_brokerResponseReady = new SemaphoreSlim(0);

            string paramsAsQuery = brokerPayload.ToQueryParameter();
            _logger.Info(iOSBrokerConstants.InvokeTheIosBroker);
            NSUrl url = new NSUrl(iOSBrokerConstants.InvokeV2Broker + paramsAsQuery);

            _logger.VerbosePii(
                iOSBrokerConstants.BrokerPayloadPii + paramsAsQuery,
                iOSBrokerConstants.BrokerPayloadNoPii + brokerPayload.Count);

            DispatchQueue.MainQueue.DispatchAsync(() => UIApplication.SharedApplication.OpenUrl(url));

            using (_logger.LogBlockDuration("waiting for broker response"))
            {
                await s_brokerResponseReady.WaitAsync().ConfigureAwait(false);
            }
        }

        private MsalTokenResponse ProcessBrokerResponse()
        {
            using (_logger.LogMethodDuration())
            {
                string[] keyValuePairs = s_brokerResponse.Query.Split('&');
                Dictionary<string, string> responseDictionary = new Dictionary<string, string>(StringComparer.InvariantCulture);

                foreach (string pair in keyValuePairs)
                {
                    string[] keyValue = pair.Split('=');
                    responseDictionary[keyValue[0]] = CoreHelpers.UrlDecode(keyValue[1]);

                    if (responseDictionary[keyValue[0]].Equals("(null)", StringComparison.OrdinalIgnoreCase)
                        && keyValue[0].Equals(iOSBrokerConstants.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        responseDictionary[iOSBrokerConstants.Error] = iOSBrokerConstants.BrokerError;

                        _logger.VerbosePii(iOSBrokerConstants.BrokerResponseValuesPii + keyValue.ToString(),
                        iOSBrokerConstants.BrokerResponseContainsError);
                    }
                }

                _logger.Verbose(iOSBrokerConstants.ProcessBrokerResponse + responseDictionary.Count);

                return ResultFromBrokerResponse(responseDictionary);
            }
        }
        private MsalTokenResponse ResultFromBrokerResponse(Dictionary<string, string> responseDictionary)
        {
            MsalTokenResponse brokerTokenResponse;

            string expectedHash = responseDictionary[iOSBrokerConstants.ExpectedHash];
            string encryptedResponse = responseDictionary[iOSBrokerConstants.EncryptedResponsed];
            string decryptedResponse = BrokerKeyHelper.DecryptBrokerResponse(encryptedResponse, _logger);
            string responseActualHash = _cryptoManager.CreateSha256Hash(decryptedResponse);
            byte[] rawHash = Convert.FromBase64String(responseActualHash);
            string hash = BitConverter.ToString(rawHash);

            if (expectedHash.Equals(hash.Replace("-", ""), StringComparison.OrdinalIgnoreCase))
            {
                responseDictionary = CoreHelpers.ParseKeyValueList(decryptedResponse, '&', false, null);

                if (!ValidateBrokerResponseNonceWithRequestNonce(responseDictionary))
                {
                    return new MsalTokenResponse
                    {
                        Error = MsalError.BrokerNonceMismatch,
                        ErrorDescription = MsalErrorMessage.BrokerNonceMismatch
                    };
                }

                if (responseDictionary.ContainsKey(iOSBrokerConstants.ApplicationToken))
                {
                    TryWriteBrokerApplicationTokenToKeychain(
                        responseDictionary[BrokerResponseConst.ClientId],
                        responseDictionary[iOSBrokerConstants.ApplicationToken]);
                }

                brokerTokenResponse = MsalTokenResponse.CreateFromiOSBrokerResponse(responseDictionary);

                if (responseDictionary.TryGetValue(BrokerResponseConst.BrokerErrorCode, out string errCode))
                {
                    if(errCode == BrokerResponseConst.iOSBrokerUserCancellationErrorCode)
                    {
                        responseDictionary[BrokerResponseConst.BrokerErrorCode] = MsalError.AuthenticationCanceledError;
                    }
                    else if (errCode == BrokerResponseConst.iOSBrokerProtectionPoliciesRequiredErrorCode)
                    {
                        responseDictionary[BrokerResponseConst.BrokerErrorCode] = MsalError.ProtectionPolicyRequired;
                    }
                }
            }
            else
            {
                brokerTokenResponse = new MsalTokenResponse
                {
                    Error = MsalError.BrokerResponseHashMismatch,
                    ErrorDescription = MsalErrorMessage.BrokerResponseHashMismatch
                };
            }

            return brokerTokenResponse;
        }

        private bool IsBrokerInstalled(string brokerUriScheme)
        {
            return UIApplication.SharedApplication.CanOpenUrl(new NSUrl(brokerUriScheme));
        }

        private bool ValidateBrokerResponseNonceWithRequestNonce(Dictionary<string, string> brokerResponseDictionary)
        {
            if (_brokerV3Installed)
            {
                string brokerResponseNonce = brokerResponseDictionary[BrokerResponseConst.iOSBrokerNonce];

                bool ok = string.Equals(
                    brokerResponseNonce,
                    _brokerRequestNonce,
                    StringComparison.InvariantCultureIgnoreCase);

                if (!ok)
                {
                    _logger.Error(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "Nonce check failed! Broker response nonce is:  {0}, \nBroker request nonce is: {1}",
                            brokerResponseNonce,
                            _brokerRequestNonce));
                }
                return ok;
            }
            return true;
        }

        private void TryWriteBrokerApplicationTokenToKeychain(string clientId, string applicationToken)
        {
            iOSTokenCacheAccessor iOSTokenCacheAccessor = new iOSTokenCacheAccessor();

            try
            {
                SecStatusCode secStatusCode = iOSTokenCacheAccessor.SaveBrokerApplicationToken(clientId, applicationToken);

                _logger.Info(string.Format(
                    CultureInfo.CurrentCulture,
                    iOSBrokerConstants.AttemptToSaveBrokerApplicationToken + "SecStatusCode: {0}",
                    secStatusCode));
            }
            catch (Exception ex)
            {
                throw new MsalClientException(
                    MsalError.WritingApplicationTokenToKeychainFailed,
                    MsalErrorMessage.WritingApplicationTokenToKeychainFailed + ex.Message);
            }
        }

        private string TryReadBrokerApplicationTokenFromKeychain(Dictionary<string, string> brokerPayload)
        {
            iOSTokenCacheAccessor iOSTokenCacheAccessor = new iOSTokenCacheAccessor();

            try
            {
                SecStatusCode secStatusCode = iOSTokenCacheAccessor.TryGetBrokerApplicationToken(brokerPayload[BrokerParameter.ClientId], out string appToken);

                _logger.Info(string.Format(
                    CultureInfo.CurrentCulture,
                    iOSBrokerConstants.SecStatusCodeFromTryGetBrokerApplicationToken + "SecStatusCode: {0}",
                    secStatusCode));

                return appToken;
            }
            catch (Exception ex)
            {
                throw new MsalClientException(
                    MsalError.ReadingApplicationTokenFromKeychainFailed,
                    MsalErrorMessage.ReadingApplicationTokenFromKeychainFailed + ex.Message);
            }
        }

        public static void SetBrokerResponse(NSUrl responseUrl)
        {
            s_brokerResponse = responseUrl;
            s_brokerResponseReady?.Release();
        }

        #region Silent Flow - not supported
        /// <summary>
        /// iOS broker does not handle silent flow
        /// </summary>
        public Task RemoveAccountAsync(ApplicationConfiguration applicationConfiguration, IAccount account)
        {
            return Task.Delay(0); // nop
        }

        /// <summary>
        /// iOS broker does not handle silent flow
        /// </summary>
        public Task<IReadOnlyList<IAccount>> GetAccountsAsync(
                    string clientID,
                    string redirectUri,
                    AuthorityInfo authorityInfo,
                    ICacheSessionManager cacheSessionManager,
                    IInstanceDiscoveryManager instanceDiscoveryManager)
        {
            return Task.FromResult(CollectionHelpers.GetEmptyReadOnlyList<IAccount>()); // nop
        }

        /// <summary>
        /// iOS broker does not handle silent flow
        /// </summary>
        public Task<MsalTokenResponse> AcquireTokenSilentAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            return Task.FromResult<MsalTokenResponse>(null); // nop
        }

        public Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            return Task.FromResult<MsalTokenResponse>(null); // nop
        }

        #endregion
    }
}
