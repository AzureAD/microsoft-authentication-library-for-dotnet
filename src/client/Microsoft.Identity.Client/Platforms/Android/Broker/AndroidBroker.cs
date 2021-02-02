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
using OperationCanceledException = Android.Accounts.OperationCanceledException;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    [AndroidNative.Runtime.Preserve(AllMembers = true)]
    internal class AndroidBroker : IBroker
    {
        public long AccountManagerTimeoutSeconds { get; } = 5 * 60;
        private static MsalTokenResponse s_androidBrokerTokenResponse = null;
        //Since the correlation ID is not returned from the broker response, it must be stored at the beginning of the authentication call and re-injected into the response at the end.
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
            return _brokerHelper.IsBrokerInstalledAndInvokable();
        }

        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {

            _brokerHelper.CheckPowerOptimizationStatus();

            s_androidBrokerTokenResponse = null;

            BrokerRequest brokerRequest = BrokerRequest.FromInteractiveParameters(
                authenticationRequestParameters, acquireTokenInteractiveParameters);

            // There can only be 1 broker request at a time so keep track of the correlation id
            s_correlationId = brokerRequest.CorrelationId;

            try
            {
                await InitiateBrokerHandshakeAsync(_parentActivity).ConfigureAwait(false);
                await AcquireTokenInteractiveViaBrokerAsync(brokerRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "Android broker interactive invocation failed. ");
                _brokerHelper.HandleBrokerOperationError(ex);
            }

            using (_logger.LogBlockDuration("Waiting for Android broker response. "))
            {
                await AndroidBrokerHelper.ReadyForResponse.WaitAsync().ConfigureAwait(false);
                return s_androidBrokerTokenResponse;
            }
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            _brokerHelper.CheckPowerOptimizationStatus();

            BrokerRequest brokerRequest = BrokerRequest.FromSilentParameters(
                authenticationRequestParameters, acquireTokenSilentParameters);

            try
            {
                await InitiateBrokerHandshakeAsync(_parentActivity).ConfigureAwait(false);
                var androidBrokerTokenResponse = await AcquireTokenSilentViaBrokerAsync(brokerRequest).ConfigureAwait(false);
                return androidBrokerTokenResponse;
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "Android broker silent invocation failed. ");
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

                _logger.Verbose("Starting Android Broker interactive authentication. ");
                Intent brokerIntent = await GetIntentForInteractiveBrokerRequestAsync(brokerRequest, _parentActivity).ConfigureAwait(false);

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
                        _logger.ErrorPiiWithPrefix(e, "Unable to get Android activity during interactive broker request. ");
                        throw;
                    }
                }
            }
        }

        public async Task<Intent> GetIntentForInteractiveBrokerRequestAsync(BrokerRequest brokerRequest, Activity callerActivity)
        {
            Intent intent = null;

            try
            {
                IAccountManagerFuture result = null;
                // Callback is not passed since it is making a blocking call to get
                // intent. Activity needs to be launched from calling app
                // to get the calling app's metadata if needed at BrokerActivity.

                Bundle addAccountOptions = new Bundle();
                addAccountOptions.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.GetIntentForInteractiveRequest);

                result = _brokerHelper.AndroidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                    BrokerConstants.AuthtokenType,
                    null,
                    addAccountOptions,
                    null,
                    null,
                    GetPreferredLooper(callerActivity));

                if (result == null)
                {
                    _logger.Info("Android account manager didn't return any results for interactive broker request. ");
                }

                Bundle bundleResult = null;

                try
                {
                    bundleResult = (Bundle)await result.GetResultAsync(
                         AccountManagerTimeoutSeconds,
                         TimeUnit.Seconds)
                         .ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.Error("An error occurred when trying to communicate with account manager: " + ex.Message);
                }
                catch (Exception ex)
                {
                    throw new MsalClientException(MsalError.BrokerApplicationRequired, MsalErrorMessage.AndroidBrokerCannotBeInvoked, ex);
                }

                intent = (Intent)bundleResult?.GetParcelable(AccountManager.KeyIntent);

                //Validate that the intent was created successfully.
                if (intent != null)
                {
                    _logger.Info("Intent created from BundleResult is not null. Starting interactive broker request. ");
                    // Need caller info UID for broker communication
                    intent.PutExtra(BrokerConstants.CallerInfoUID, Binder.CallingUid);
                }
                else
                {
                    _logger.Info("Intent created from BundleResult is null. ");
                    throw new MsalClientException(MsalError.NullIntentReturnedFromAndroidBroker, MsalErrorMessage.NullIntentReturnedFromBroker);
                }

                intent = GetInteractiveBrokerIntent(brokerRequest, intent);
            }
            catch
            {
                _logger.Error("Error when trying to acquire intent for broker authentication. ");
                throw;
            }

            return intent;
        }

        private Intent GetInteractiveBrokerIntent(BrokerRequest brokerRequest, Intent brokerIntent)
        {
            _brokerHelper.ValidateBrokerRedirectURI(brokerRequest);
            string brokerRequestJson = JsonHelper.SerializeToJson(brokerRequest);
            _logger.InfoPii("GetInteractiveBrokerIntent: " + brokerRequestJson, "Enable PII to see the broker request. ");
            brokerIntent.PutExtra(BrokerConstants.BrokerRequestV2, brokerRequestJson);

            return brokerIntent;
        }

        private async Task<MsalTokenResponse> AcquireTokenSilentViaBrokerAsync(BrokerRequest brokerRequest)
        {
            // Don't send silent background request if account information is not provided

            using (_logger.LogMethodDuration())
            {
                _logger.Verbose("User is specified for silent token request. Starting silent Android broker request. ");
                string silentResult = await GetBrokerAuthTokenSilentlyAsync(brokerRequest, _parentActivity).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(silentResult))
                {
                    return MsalTokenResponse.CreateFromAndroidBrokerResponse(silentResult, brokerRequest.CorrelationId);
                }

                return new MsalTokenResponse
                {
                    Error = MsalError.BrokerResponseReturnedError,
                    ErrorDescription = "Unknown Android broker error. Failed to acquire token silently from the broker. " + MsalErrorMessage.AndroidBrokerCannotBeInvoked,
                };
            }
        }

        public async Task<string> GetBrokerAuthTokenSilentlyAsync(BrokerRequest brokerRequest, Activity callerActivity)
        {
            brokerRequest = UpdateRequestWithAccountInfo(brokerRequest, callerActivity);
            Bundle silentOperationBundle = _brokerHelper.CreateSilentBrokerBundle(brokerRequest);
            silentOperationBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.AcquireTokenSilent);

            return await PerformAcquireTokenSilentAsync(silentOperationBundle).ConfigureAwait(false);
        }

        private async Task<string> PerformAcquireTokenSilentAsync(Bundle silentOperationBundle)
        {
            IAccountManagerFuture result = _brokerHelper.AndroidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                                            BrokerConstants.AuthtokenType,
                                            null,
                                            silentOperationBundle,
                                            null,
                                            null,
                                            GetPreferredLooper(_parentActivity));

            if (result != null)
            {
                Bundle bundleResult = null;

                try
                {
                    bundleResult = (Bundle)await result.GetResultAsync(
                         AccountManagerTimeoutSeconds,
                         TimeUnit.Seconds)
                         .ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.Error("An error occurred when trying to communicate with the account manager: " + ex.Message);
                }
                catch (Exception ex)
                {
                    throw new MsalClientException(MsalError.BrokerApplicationRequired, MsalErrorMessage.AndroidBrokerCannotBeInvoked, ex);
                }

                string responseJson = bundleResult.GetString(BrokerConstants.BrokerResultV2);

                bool success = bundleResult.GetBoolean(BrokerConstants.BrokerRequestV2Success);
                _logger.Info($"Android Broker Silent call result - success? {success}. ");

                if (!success)
                {
                    _logger.Warning($"Android Broker Silent call failed. " +
                        $"This usually means that the RT cannot be refreshed and interaction is required. " +
                        $"BundleResult: {bundleResult} Result string: {responseJson}");
                }

                // upstream logic knows how to extract potential errors from this result
                return responseJson;
            }

            _logger.Info("Android Broker didn't return any results. ");
            return null;
        }

        /// <summary>
        /// This method is only used for Silent authentication requests so that we can check to see if an account exists in the account manager before
        /// sending the silent request to the broker. 
        /// </summary>
        public BrokerRequest UpdateRequestWithAccountInfo(BrokerRequest brokerRequest, Activity callerActivity)
        {
            var accounts = GetBrokerAccounts(brokerRequest, callerActivity);

            if (string.IsNullOrEmpty(accounts))
            {
                _logger.Info("Android account manager didn't return any accounts. ");
                throw new MsalUiRequiredException(MsalError.NoAndroidBrokerAccountFound, MsalErrorMessage.NoAndroidBrokerAccountFound);
            }

            return _brokerHelper.UpdateBrokerRequestWithAccountData(accounts, brokerRequest);
        }

        private string GetBrokerAccounts(BrokerRequest brokerRequest, Activity callerActivity)
        {
            Bundle getAccountsBundle = _brokerHelper.CreateBrokerAccountBundle(brokerRequest);
            getAccountsBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.GetAccounts);

            //This operation will acquire all of the accounts in the account manager for the given client ID
            var result = _brokerHelper.AndroidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                BrokerConstants.AuthtokenType,
                null,
                getAccountsBundle,
                null,
                null,
                GetPreferredLooper(callerActivity));

            Bundle bundleResult = (Bundle)result?.Result;
            return bundleResult?.GetString(BrokerConstants.BrokerAccounts);
        }

        public void HandleInstallUrl(string appLink)
        {
            _brokerHelper.HandleInstallUrl(appLink, _parentActivity);
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID, string redirectUri)
        {
            using (_logger.LogMethodDuration())
            {
                if (!IsBrokerInstalledAndInvokable())
                {
                    _logger.Warning("Android broker is either not installed or is not reachable so no accounts will be returned. ");
                    return new List<IAccount>();
                }

                BrokerRequest brokerRequest = new BrokerRequest() { ClientId = clientID, RedirectUri = new Uri(redirectUri) };

                try
                {
                    await InitiateBrokerHandshakeAsync(_parentActivity).ConfigureAwait(false);

                    var accounts = GetBrokerAccounts(brokerRequest, _parentActivity);

                    return _brokerHelper.ExtractBrokerAccountsFromAccountData(accounts);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to get Android broker accounts from the broker. ");
                    _brokerHelper.HandleBrokerOperationError(ex);
                    throw;
                }
            }
        }

        public async Task RemoveAccountAsync(IApplicationConfiguration applicationConfiguration, IAccount account)
        {
            using (_logger.LogMethodDuration())
            {
                if (!IsBrokerInstalledAndInvokable())
                {
                    _logger.Warning("Android broker is either not installed or not reachable so no accounts will be removed. ");
                    return;
                }

                try
                {
                    await InitiateBrokerHandshakeAsync(_parentActivity).ConfigureAwait(false);
                    RemoveBrokerAccountInAccountManager(applicationConfiguration.ClientId, account);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to remove Android broker account from the broker. ");
                    _brokerHelper.HandleBrokerOperationError(ex);
                    throw;
                }
            }
        }

        public void RemoveBrokerAccountInAccountManager(string clientId, IAccount account)
        {
            Bundle removeAccountBundle = _brokerHelper.CreateRemoveBrokerAccountBundle(clientId, account);
            removeAccountBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.RemoveAccount);

            _brokerHelper.AndroidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                BrokerConstants.AuthtokenType,
                null,
                removeAccountBundle,
                null,
                null,
                GetPreferredLooper(null));
        }

        public Handler GetPreferredLooper(Activity callerActivity)
        {
            var myLooper = Looper.MyLooper();
            if (myLooper != null && callerActivity != null && callerActivity.MainLooper != myLooper)
            {
                _logger.Info("myLooper returned. Calling thread is associated with a Looper: " + myLooper.ToString());
                return new Handler(myLooper);
            }
            else
            {
                _logger.Info("Looper.MainLooper returned: " + Looper.MainLooper.ToString());
                return new Handler(Looper.MainLooper);
            }
        }

        //In order for broker to use the V2 endpoint during authentication, MSAL must initiate a handshake with broker to specify what endpoint should be used for the request.
        public async Task InitiateBrokerHandshakeAsync(Activity callerActivity)
        {
            using (_logger.LogMethodDuration())
            {
                try
                {
                    Bundle helloRequestBundle = _brokerHelper.GetHandshakeOperationBundle();

                    IAccountManagerFuture result = _brokerHelper.AndroidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                        BrokerConstants.AuthtokenType,
                        null,
                        helloRequestBundle,
                        null,
                        null,
                        GetPreferredLooper(callerActivity));

                    if (result != null)
                    {
                        Bundle bundleResult = null;

                        try
                        {
                            bundleResult = (Bundle)await result.GetResultAsync(
                                AccountManagerTimeoutSeconds,
                                TimeUnit.Seconds)
                                .ConfigureAwait(false);
                        }
                        catch (System.OperationCanceledException ex)
                        {
                            _logger.Error("An error occurred when trying to communicate with the account manager: " + ex.Message);
                        }

                        var bpKey = bundleResult?.GetString(BrokerConstants.NegotiatedBPVersionKey);

                        if (!string.IsNullOrEmpty(bpKey))
                        {
                            _logger.Info("Using broker protocol version: " + bpKey);
                            return;
                        }

                        dynamic errorResult = JObject.Parse(bundleResult?.GetString(BrokerConstants.BrokerResultV2));
                        string errorCode = null;
                        string errorDescription = null;

                        if (!string.IsNullOrEmpty(errorResult))
                        {
                            errorCode = errorResult[BrokerResponseConst.BrokerErrorCode]?.ToString();
                            string errorMessage = errorResult[BrokerResponseConst.BrokerErrorMessage]?.ToString();
                            errorDescription = $"An error occurred during hand shake with the broker. Error: {errorCode} Error Message: {errorMessage}";
                        }
                        else
                        {
                            errorCode = BrokerConstants.BrokerUnknownErrorCode;
                            errorDescription = "An error occurred during hand shake with the broker, no detailed error information was returned. ";
                        }

                        _logger.Error(errorDescription);
                        throw new MsalClientException(errorCode, errorDescription);
                    }

                    throw new MsalClientException("Could not communicate with broker via account manager. Please ensure power optimization settings are turned off. ");
                }
                catch (Exception ex)
                {
                    _logger.Error("Error when trying to initiate communication with the broker. ");
                    if (ex is MsalException)
                    {
                        throw;
                    }

                    throw new MsalClientException(MsalError.BrokerApplicationRequired, MsalErrorMessage.AndroidBrokerCannotBeInvoked, ex);
                }
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
    }
}
