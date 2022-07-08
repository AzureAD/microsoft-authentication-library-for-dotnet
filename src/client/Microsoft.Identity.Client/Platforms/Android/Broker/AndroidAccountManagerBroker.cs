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
using Microsoft.Identity.Client.Http;
using System.Net;
using Android.OS;
using System.Linq;
using Android.Accounts;
using Java.Util.Concurrent;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance.Discovery;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
#if MAUI
    [Preserve(AllMembers = true)]
#else
    [AndroidNative.Runtime.Preserve(AllMembers = true)]
#endif
    internal class AndroidAccountManagerBroker : IBroker
    {
        private long AccountManagerTimeoutSeconds { get; } = 5 * 60;
        private readonly AndroidBrokerHelper _brokerHelper;
        private readonly ILoggerAdapter _logger;
        private readonly Activity _parentActivity;

        public bool IsPopSupported => false;

        public AndroidAccountManagerBroker(CoreUIParent uiParent, ILoggerAdapter logger)
        {
            _parentActivity = uiParent?.Activity;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AuthenticationContinuationHelper.LastRequestLogger = _logger;
            _brokerHelper = new AndroidBrokerHelper(Application.Context, logger);
        }

        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
            return _brokerHelper.IsBrokerInstalledAndInvokable(authorityType);
        }

        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {

            CheckPowerOptimizationStatus();

            AndroidBrokerInteractiveResponseHelper.InteractiveBrokerTokenResponse = null;

            BrokerRequest brokerRequest = BrokerRequest.FromInteractiveParameters(
                authenticationRequestParameters, acquireTokenInteractiveParameters);

            // There can only be 1 broker request at a time so keep track of the correlation id
            AndroidBrokerInteractiveResponseHelper.InteractiveRequestCorrelationId = brokerRequest.CorrelationId;

            try
            {
                await InitiateBrokerHandshakeAsync().ConfigureAwait(false);
                await AcquireTokenInteractiveViaBrokerAsync(brokerRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "[Android broker] Android broker interactive invocation failed. ");
                _brokerHelper.HandleBrokerOperationError(ex);
            }

            using (_logger.LogBlockDuration("[Android broker] Waiting for Android broker response. "))
            {
                await AndroidBrokerInteractiveResponseHelper.ReadyForResponse.WaitAsync().ConfigureAwait(false);
                return AndroidBrokerInteractiveResponseHelper.InteractiveBrokerTokenResponse;
            }
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            CheckPowerOptimizationStatus();

            BrokerRequest brokerRequest = BrokerRequest.FromSilentParameters(
                authenticationRequestParameters, acquireTokenSilentParameters);

            try
            {
                await InitiateBrokerHandshakeAsync().ConfigureAwait(false);
                return await AcquireTokenSilentViaBrokerAsync(brokerRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "[Android broker] Android broker silent invocation failed. ");
                _brokerHelper.HandleBrokerOperationError(ex);
                throw;
            }
        }

        private async Task AcquireTokenInteractiveViaBrokerAsync(BrokerRequest brokerRequest)
        {
            using (_logger.LogMethodDuration())
            {
                // onActivityResult will receive the response for this activity.
                // Launching this activity will switch to the broker app.

                _logger.Verbose("[Android broker] Starting Android Broker interactive authentication. ");
                Intent brokerIntent = await GetIntentForInteractiveBrokerRequestAsync(brokerRequest).ConfigureAwait(false);

                if (brokerIntent != null)
                {
                    _brokerHelper.LaunchInteractiveActivity(_parentActivity, brokerIntent);
                }
            }
        }

        private async Task<Intent> GetIntentForInteractiveBrokerRequestAsync(BrokerRequest brokerRequest)
        {
            try
            {
                // Callback is not passed since it is making a blocking call to get
                // intent. Activity needs to be launched from calling app
                // to get the calling app's meta data if needed at BrokerActivity.

                Bundle addAccountOptions = new Bundle();
                addAccountOptions.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.GetIntentForInteractiveRequest);

                Bundle accountManagerResult = await PerformAccountManagerOperationAsync(addAccountOptions).ConfigureAwait(false);

                return CreateIntentFromInteractiveBundle(accountManagerResult, brokerRequest);
            }
            catch
            {
                _logger.Error("[Android broker] Error when trying to acquire intent for broker authentication. ");
                throw;
            }
        }

        private Intent CreateIntentFromInteractiveBundle(Bundle accountManagerResult, BrokerRequest brokerRequest)
        {
            if (accountManagerResult == null)
            {
                _logger.Info("[Android broker] Android account manager didn't return any results for interactive broker request. ");
            }

            Intent interactiveIntent = (Intent)accountManagerResult?.GetParcelable(AccountManager.KeyIntent);

            // Validate that the intent was created successfully.
            if (interactiveIntent != null)
            {
                _logger.Info("[Android broker] Intent created from BundleResult is not null. Starting interactive broker request. ");
                // Need caller info UID for broker communication
                interactiveIntent.PutExtra(BrokerConstants.CallerInfoUID, Binder.CallingUid);
            }
            else
            {
                _logger.Info("[Android broker] Intent created from BundleResult is null. ");
                throw new MsalClientException(MsalError.NullIntentReturnedFromAndroidBroker, MsalErrorMessage.NullIntentReturnedFromBroker);
            }

            return CreateInteractiveBrokerIntent(brokerRequest, interactiveIntent);
        }

        private Intent CreateInteractiveBrokerIntent(BrokerRequest brokerRequest, Intent brokerIntent)
        {
            _brokerHelper.ValidateBrokerRedirectUri(brokerRequest);
            string brokerRequestJson = JsonHelper.SerializeToJson(brokerRequest);
            _logger.InfoPii("[Android broker] GetInteractiveBrokerIntent: " + brokerRequestJson, "Enable PII to see the broker request. ");
            brokerIntent.PutExtra(BrokerConstants.BrokerRequestV2, brokerRequestJson);

            return brokerIntent;
        }

        private async Task<MsalTokenResponse> AcquireTokenSilentViaBrokerAsync(BrokerRequest brokerRequest)
        {
            // Don't send silent background request if account information is not provided
            using (_logger.LogMethodDuration())
            {
                _logger.Verbose("[Android broker] User is specified for silent token request. Starting silent Android broker request. ");
                string silentResult = await GetBrokerAuthTokenSilentlyAsync(brokerRequest).ConfigureAwait(false);
                return _brokerHelper.HandleSilentAuthenticationResult(silentResult, brokerRequest.CorrelationId);
            }
        }

        private async Task<string> GetBrokerAuthTokenSilentlyAsync(BrokerRequest brokerRequest)
        {
            brokerRequest = UpdateRequestWithAccountInfo(brokerRequest);
            Bundle silentOperationBundle = _brokerHelper.CreateSilentBrokerBundle(brokerRequest);
            silentOperationBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.AcquireTokenSilent);

            return await AcquireTokenSilentInternalAsync(silentOperationBundle).ConfigureAwait(false);
        }

        private async Task<string> AcquireTokenSilentInternalAsync(Bundle silentOperationBundle)
        {
            Bundle accountManagerResult = await PerformAccountManagerOperationAsync(silentOperationBundle).ConfigureAwait(false);

            if (accountManagerResult != null)
            {
                return _brokerHelper.GetSilentResultFromBundle(accountManagerResult);
            }

            _logger.Info("[Android broker] Android broker didn't return any results. ");
            return null;
        }

        /// <summary>
        /// This method is only used for Silent authentication requests so that we can check to see if an account exists in the account manager before
        /// sending the silent request to the broker. 
        /// </summary>
        private BrokerRequest UpdateRequestWithAccountInfo(BrokerRequest brokerRequest)
        {
            var accounts = GetBrokerAccounts(brokerRequest);

            if (string.IsNullOrEmpty(accounts))
            {
                _logger.Info("[Android broker] Android account manager didn't return any accounts. ");
                throw new MsalUiRequiredException(MsalError.NoAndroidBrokerAccountFound, MsalErrorMessage.NoAndroidBrokerAccountFound);
            }

            return _brokerHelper.UpdateBrokerRequestWithAccountData(accounts, brokerRequest);
        }

        private string GetBrokerAccounts(BrokerRequest brokerRequest)
        {
            Bundle getAccountsBundle = _brokerHelper.CreateBrokerAccountBundle(brokerRequest);
            getAccountsBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.GetAccounts);

            //This operation will acquire all of the accounts in the account manager for the given client ID
            Bundle bundleResult = PerformAccountManagerOperationAsync(getAccountsBundle).Result;
            return bundleResult?.GetString(BrokerConstants.BrokerAccounts);
        }

        public void HandleInstallUrl(string appLink)
        {
            _brokerHelper.HandleInstallUrl(appLink, _parentActivity);
        }

        public async Task<IReadOnlyList<IAccount>> GetAccountsAsync(
            string clientId,
            string redirectUri,
            AuthorityInfo authorityInfo,
            ICacheSessionManager cacheSessionManager,
            IInstanceDiscoveryManager instanceDiscoveryManager)
        {
            using (_logger.LogMethodDuration())
            {
                if (!IsBrokerInstalledAndInvokable(authorityInfo.AuthorityType))
                {
                    _logger.Warning("[Android broker] Broker is either not installed or is not reachable so no accounts will be returned. ");
                    return CollectionHelpers.GetEmptyReadOnlyList<IAccount>();
                }

                BrokerRequest brokerRequest = new BrokerRequest() { ClientId = clientId, RedirectUri = new Uri(redirectUri) };

                try
                {
                    await InitiateBrokerHandshakeAsync().ConfigureAwait(false);

                    var accounts = GetBrokerAccounts(brokerRequest);

                    return _brokerHelper.ExtractBrokerAccountsFromAccountData(accounts);
                }
                catch (Exception ex)
                {
                    _logger.Error("[Android broker] Failed to get Android broker accounts from the broker. ");
                    _brokerHelper.HandleBrokerOperationError(ex);
                    throw;
                }
            }
        }

        public async Task RemoveAccountAsync(ApplicationConfiguration appConfig, IAccount account)
        {
            using (_logger.LogMethodDuration())
            {
                if (!IsBrokerInstalledAndInvokable(appConfig.Authority.AuthorityInfo.AuthorityType))
                {
                    _logger.Warning("[Android broker] Broker is either not installed or not reachable so no accounts will be removed. ");
                    return;
                }

                try
                {
                    await InitiateBrokerHandshakeAsync().ConfigureAwait(false);
                    await RemoveBrokerAccountInAccountManagerAsync(appConfig.ClientId, account).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error("[Android broker] Failed to remove broker account from the broker. ");
                    _brokerHelper.HandleBrokerOperationError(ex);
                    throw;
                }
            }
        }

        private async Task RemoveBrokerAccountInAccountManagerAsync(string clientId, IAccount account)
        {
            Bundle removeAccountBundle = _brokerHelper.CreateRemoveBrokerAccountBundle(clientId, account);
            removeAccountBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.RemoveAccount);

            await PerformAccountManagerOperationAsync(removeAccountBundle).ConfigureAwait(false);
        }

        private Handler GetPreferredLooper(Activity callerActivity)
        {
            var myLooper = Looper.MyLooper();
            if (myLooper != null && callerActivity != null && callerActivity.MainLooper != myLooper)
            {
                _logger.Info("[Android broker] myLooper returned. Calling thread is associated with a Looper: " + myLooper.ToString());
                return new Handler(myLooper);
            }
            else
            {
                _logger.Info("[Android broker] Looper.MainLooper returned: " + Looper.MainLooper.ToString());
                return new Handler(Looper.MainLooper);
            }
        }

        //In order for broker to use the V2 endpoint during authentication, MSAL must initiate a handshake with broker to specify what endpoint should be used for the request.
        public async Task InitiateBrokerHandshakeAsync()
        {
            using (_logger.LogMethodDuration())
            {
                try
                {
                    Bundle helloRequestBundle = _brokerHelper.CreateHandShakeOperationBundle();

                    Bundle helloRequestResult = await PerformAccountManagerOperationAsync(helloRequestBundle).ConfigureAwait(false);

                    if (helloRequestResult != null)
                    {
                        var bpKey = helloRequestResult.GetString(BrokerConstants.NegotiatedBPVersionKey);

                        if (!string.IsNullOrEmpty(bpKey))
                        {
                            _logger.Info("[Android broker] Using broker protocol version: " + bpKey);
                            return;
                        }

                        dynamic errorResult = JObject.Parse(helloRequestResult.GetString(BrokerConstants.BrokerResultV2));
                        string errorCode = null;
                        string errorDescription = null;

                        if (!string.IsNullOrEmpty(errorResult))
                        {
                            errorCode = errorResult[BrokerResponseConst.BrokerErrorCode]?.ToString();
                            string errorMessage = errorResult[BrokerResponseConst.BrokerErrorMessage]?.ToString();
                            errorDescription = $"[Android broker] An error occurred during hand shake with the broker. Error: {errorCode} Error Message: {errorMessage}";
                        }
                        else
                        {
                            errorCode = BrokerConstants.BrokerUnknownErrorCode;
                            errorDescription = "[Android broker] An error occurred during hand shake with the broker, no detailed error information was returned. ";
                        }

                        _logger.Error(errorDescription);
                        throw new MsalClientException(errorCode, errorDescription);
                    }

                    throw new MsalClientException("[Android broker] Could not communicate with broker via account manager. Please ensure power optimization settings are turned off. ");
                }
                catch (Exception ex)
                {
                    _logger.Error("[Android broker] Error when trying to initiate communication with the broker. ");
                    if (ex is MsalException)
                    {
                        throw;
                    }

                    throw new MsalClientException(MsalError.BrokerApplicationRequired, MsalErrorMessage.AndroidBrokerCannotBeInvoked, ex);
                }
            }
        }

        private async Task<Bundle> PerformAccountManagerOperationAsync(Bundle requestBundle)
        {
            IAccountManagerFuture accountManagerResult = _brokerHelper.AndroidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                                            BrokerConstants.AuthtokenType,
                                            null,
                                            requestBundle,
                                            null,
                                            null,
                                            GetPreferredLooper(_parentActivity));

            return await ExtractAccountManagerResultAsync(accountManagerResult).ConfigureAwait(false);
        }

        private async Task<Bundle> ExtractAccountManagerResultAsync(IAccountManagerFuture accountManagerResultFuture)
        {
            if (accountManagerResultFuture != null)
            {
                try
                {
                    return (Bundle)await accountManagerResultFuture.GetResultAsync(
                        AccountManagerTimeoutSeconds,
                        TimeUnit.Seconds)
                        .ConfigureAwait(false);
                }
                catch (System.OperationCanceledException ex)
                {
                    _logger.Error("[Android broker] An error occurred when trying to communicate with the account manager: " + ex.Message);
                }
            }

            throw new MsalClientException("[Android broker] Could not communicate with broker via account manager. Please ensure power optimization settings are turned off. ");
        }

        /// Check if the network is available.
        private void CheckPowerOptimizationStatus()
        {
            CheckPackageForPowerOptimization(Application.Context.PackageName);
            CheckPackageForPowerOptimization(_brokerHelper.Authenticator.PackageName);
        }

        private void CheckPackageForPowerOptimization(string package)
        {
            var powerManager = PowerManager.FromContext(Application.Context);

            //Power optimization checking was added in API 23
            if ((int)Build.VERSION.SdkInt >= (int)BuildVersionCodes.M &&
                powerManager.IsDeviceIdleMode &&
                !powerManager.IsIgnoringBatteryOptimizations(package))
            {
                _logger.Error("[Android broker] Power optimization detected for the application: " + package + " and the device is in doze mode or the app is in standby. \n" +
                    "Please disable power optimizations for this application to authenticate.");
            }
        }

        /// <summary>
        /// Android Broker does not support logging in a "default" user.
        /// </summary>
        public Task<MsalTokenResponse> AcquireTokenSilentDefaultUserAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            throw new MsalUiRequiredException(
                       MsalError.CurrentBrokerAccount,
                       MsalErrorMessage.MsalUiRequiredMessage,
                       null,
                       UiRequiredExceptionClassification.AcquireTokenSilentFailed);
        }

        public Task<MsalTokenResponse> AcquireTokenByUsernamePasswordAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenByUsernamePasswordParameters acquireTokenByUsernamePasswordParameters)
        {
            return Task.FromResult<MsalTokenResponse>(null); // nop
        }
    }
}
