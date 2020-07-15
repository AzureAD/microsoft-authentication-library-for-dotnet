// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using AndroidNative = Android;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.ApiConfig.Parameters;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    [AndroidNative.Runtime.Preserve(AllMembers = true)]
    internal class AndroidBroker : IBroker
    {
        // When the broker responds, we cannot correlate back to a started task. 
        // So we make a simplifying assumption - only one broker open session can exist at a time
        // This semaphore is static to enforce this
        private static SemaphoreSlim s_readyForResponse = new SemaphoreSlim(0);

        private static MsalTokenResponse s_androidBrokerTokenResponse = null;
        //Since the correlation ID is not returned from the broker response, it must be stored at the beginning of the authentication call and reinjected into the response at the end.
        private static string s_correlationId;
        private readonly AndroidBrokerHelper _brokerHelper;
        private readonly ICoreLogger _logger;
        private readonly Activity _parentActivity;

        public AndroidBroker(CoreUIParent uiParent, ICoreLogger logger)
        {
            _parentActivity = uiParent?.Activity;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AuthenticationContinuationHelper.LastRequestLogger = _logger;
            _brokerHelper = new AndroidBrokerHelper(Application.Context, logger);
        }

        public bool IsBrokerInstalledAndInvokable()
        {
            using (_logger.LogMethodDuration())
            {
                bool canInvoke = _brokerHelper.CanSwitchToBroker();
                _logger.Verbose("Can invoke broker? " + canInvoke);

                return canInvoke;
            }
        }

        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            s_androidBrokerTokenResponse = null;

            BrokerRequest brokerRequest = BrokerRequest.FromInteractiveParameters(
                authenticationRequestParameters, acquireTokenInteractiveParameters);

            // There can only be 1 broker request at a time so keep track of the correlation id
            s_correlationId = brokerRequest.CorrelationId;

            try
            {
                await _brokerHelper.InitiateBrokerHandshakeAsync(_parentActivity).ConfigureAwait(false);

                // todo: needed? 
                // brokerPayload[BrokerParameter.BrokerAccountName] = AndroidBrokerHelper.GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username);              

                await AcquireTokenInteractiveViaBrokerAsync(brokerRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "Broker interactive invocation failed.");
                HandleBrokerOperationError(ex);
            }

            using (_logger.LogBlockDuration("waiting for broker response"))
            {                
                await s_readyForResponse.WaitAsync().ConfigureAwait(false);
                return s_androidBrokerTokenResponse;
            }
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {

            BrokerRequest brokerRequest = BrokerRequest.FromSilentParameters(
                authenticationRequestParameters, acquireTokenSilentParameters);

            try
            {
                await _brokerHelper.InitiateBrokerHandshakeAsync(_parentActivity).ConfigureAwait(false);
                var androidBrokerTokenResponse = await AcquireTokenSilentViaBrokerAsync(brokerRequest).ConfigureAwait(false);
                return androidBrokerTokenResponse;
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "Broker silent invocation failed.");
                HandleBrokerOperationError(ex);
                throw;
            }
        }      

        private async Task AcquireTokenInteractiveViaBrokerAsync(BrokerRequest brokerRequest)
        {
            using (_logger.LogMethodDuration())
            {
                // onActivityResult will receive the response for this activity.
                // Lauching this activity will switch to the broker app.

                _logger.Verbose("Starting Android Broker interactive authentication");
                Intent brokerIntent = await _brokerHelper
                    .GetIntentForInteractiveBrokerRequestAsync(brokerRequest, _parentActivity)
                    .ConfigureAwait(false);

                if (brokerIntent != null)
                {
                    try
                    {
                        _logger.Info(
                            "Calling activity pid:" + AndroidNative.OS.Process.MyPid()
                            + " tid:" + AndroidNative.OS.Process.MyTid() + "uid:"
                            + AndroidNative.OS.Process.MyUid());

                        _parentActivity.StartActivityForResult(brokerIntent, 1001);
                    }
                    catch (ActivityNotFoundException e)
                    {
                        _logger.ErrorPiiWithPrefix(e, "Unable to get android activity during interactive broker request");
                        throw;
                    }
                }
            }
        }

        private async Task<MsalTokenResponse> AcquireTokenSilentViaBrokerAsync(BrokerRequest brokerRequest)
        {
            // Don't send silent background request if account information is not provided

            using (_logger.LogMethodDuration())
            {
                _logger.Verbose("User is specified for silent token request. Starting silent broker request.");
                string silentResult = await _brokerHelper.GetBrokerAuthTokenSilentlyAsync(brokerRequest, _parentActivity).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(silentResult))
                {
                    return MsalTokenResponse.CreateFromAndroidBrokerResponse(silentResult, brokerRequest.CorrelationId);
                }

                return new MsalTokenResponse
                {
                    Error = MsalError.BrokerResponseReturnedError,
                    ErrorDescription = "Failed to acquire token silently from the broker." + MsalErrorMessage.AndroidBrokerCannotBeInvoked,
                };
            }
        }

        public void HandleInstallUrl(string appLink)
        {
            _logger.Info("Android Broker - Starting ActionView activity to " + appLink);
            _parentActivity.StartActivity(new Intent(Intent.ActionView, AndroidNative.Net.Uri.Parse(appLink)));

            throw new MsalClientException(
                MsalError.BrokerApplicationRequired,
                MsalErrorMessage.BrokerApplicationRequired);
        }

        internal static void SetBrokerResult(Intent data, int resultCode, ICoreLogger unreliableLogger)
        {
            try
            {
                if (data == null)
                {
                    unreliableLogger?.Info("Data is null, stopping.");
                    return;
                }

                switch (resultCode)
                {
                    case (int)BrokerResponseCode.ResponseReceived:
                        unreliableLogger?.Info("Response received, decoding...");

                        s_androidBrokerTokenResponse =
                            MsalTokenResponse.CreateFromAndroidBrokerResponse(
                                data.GetStringExtra(BrokerConstants.BrokerResultV2),
                                s_correlationId);
                        break;
                    case (int)BrokerResponseCode.UserCancelled:
                        unreliableLogger?.Info("Response received - user cancelled");

                        s_androidBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = MsalError.AuthenticationCanceledError,
                            ErrorDescription = MsalErrorMessage.AuthenticationCanceled,
                        };
                        break;
                    case (int)BrokerResponseCode.BrowserCodeError:
                        unreliableLogger?.Info("Response received - error ");

                        dynamic errorResult = JObject.Parse(data.GetStringExtra(BrokerConstants.BrokerResultV2));
                        string error = null;
                        string errorDescription = null;

                        if (errorResult != null)
                        {
                            error = errorResult[BrokerResponseConst.BrokerErrorCode]?.ToString();
                            errorDescription = errorResult[BrokerResponseConst.BrokerErrorMessage]?.ToString();

                            unreliableLogger?.Error($"error: {error} errorDescription {errorDescription}");
                        }
                        else
                        {
                            error = BrokerConstants.BrokerUnknownErrorCode;
                            errorDescription = "Error Code received, but no error could be extracted";
                            unreliableLogger?.Error("Error response received, but not error could be extracted");
                        }

                        s_androidBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = error,
                            ErrorDescription = errorDescription,
                        };
                        break;
                    default:
                        unreliableLogger?.Error("Unkwown broker response");
                        s_androidBrokerTokenResponse = new MsalTokenResponse
                        {
                            Error = BrokerConstants.BrokerUnknownErrorCode,
                            ErrorDescription = "Broker result not returned from android broker.",
                        };
                        break;
                }
            }
            finally
            {
                s_readyForResponse.Release();
            }
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID, string redirectUri)
        {
            using (_logger.LogMethodDuration())
            {
                if (!IsBrokerInstalledAndInvokable())
                {
                    _logger.Error("Android broker is not installed so no accounts will be returned.");
                    return new List<IAccount>();
                }

                BrokerRequest brokerRequest = new BrokerRequest() { ClientId = clientID, RedirectUri = new Uri(redirectUri) };

                try
                {
                    await _brokerHelper.InitiateBrokerHandshakeAsync(_parentActivity).ConfigureAwait(false);

                    return _brokerHelper.GetBrokerAccountsInAccountManager(brokerRequest);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to get Android broker accounts from the broker.");
                    HandleBrokerOperationError(ex);
                    throw;
                }
            }
        }

        public async Task RemoveAccountAsync(string clientID, IAccount account)
        {
            using (_logger.LogMethodDuration())
            {
                if (!IsBrokerInstalledAndInvokable())
                {
                    _logger.Error("Android broker is not installed so no accounts will be removed.");
                    return;
                }
           
                try
                {
                    await _brokerHelper.InitiateBrokerHandshakeAsync(_parentActivity).ConfigureAwait(false);
                    _brokerHelper.RemoveBrokerAccountInAccountManager(clientID, account);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to remove Android broker account from the broker.");
                    HandleBrokerOperationError(ex);
                    throw;
                }
            }
        }

        private void HandleBrokerOperationError(Exception ex)
        {
            _logger.Error(MsalErrorMessage.AndroidBrokerCannotBeInvoked);
            if (ex is MsalException)
                throw ex;
            else
                throw new MsalClientException(MsalError.AndroidBrokerOperationFailed, ex.Message, ex);
        }
    }
}
