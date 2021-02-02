// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using AndroidNative = Android;
using AndroidUri = Android.Net.Uri;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    [AndroidNative.Runtime.Preserve(AllMembers = true)]
    internal class AndroidContentProviderBroker : IBroker
    {
        private readonly AndroidBrokerHelper _brokerHelper;
        private readonly ICoreLogger _logger;
        private Activity _parentActivity;
        private string _negotiatedBrokerProtocalKey = String.Empty;

        public AndroidContentProviderBroker(CoreUIParent uiParent, ICoreLogger logger)
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

        public async void InitiateBrokerHandshakeAsync()
        {
            using (_logger.LogMethodDuration())
            {
                try
                {
                    Bundle HandshakeBundleResult = await GetHandshakeBundleResultFromBrokerAsync().ConfigureAwait(false);

                    _negotiatedBrokerProtocalKey = _brokerHelper.GetProtocalKeyFromHandshakeResult(HandshakeBundleResult);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        private async Task<Bundle> GetHandshakeBundleResultFromBrokerAsync()
        {
            var bundle = _brokerHelper.GetHandshakeOperationBundle();
            var OperationBundleJSON = _brokerHelper.SearializeBundleToJSON(bundle);
            return await PerformContentResolverOperationAsync(ContentResolverOperation.hello, OperationBundleJSON).ConfigureAwait(false);
        }

        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            _brokerHelper.CheckPowerOptimizationStatus();

            InitiateBrokerHandshakeAsync();

            var brokerRequest = PrepareInteractiveBrokerRequest(authenticationRequestParameters, acquireTokenInteractiveParameters);

            return await PerformAcquireTokenInteractiveAsync(brokerRequest).ConfigureAwait(false);
        }

        private BrokerRequest PrepareInteractiveBrokerRequest(AuthenticationRequestParameters authenticationRequestParameters,
                                                              AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            BrokerRequest brokerRequest = BrokerRequest.FromInteractiveParameters(
                authenticationRequestParameters, acquireTokenInteractiveParameters);

            // There can only be 1 broker request at a time so keep track of the correlation id
            AndroidBrokerHelper.CorrelationId = brokerRequest.CorrelationId;

            return brokerRequest;
        }

        private async Task<MsalTokenResponse> PerformAcquireTokenInteractiveAsync(BrokerRequest brokerRequest)
        {
            try
            {
                await AcquireTokenInteractiveViaContentProviderAsync(brokerRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "Android broker interactive invocation failed. ");
                _brokerHelper.HandleBrokerOperationError(ex);
            }

            using (_logger.LogBlockDuration("Waiting for Android broker response. "))
            {
                await AndroidBrokerHelper.ReadyForResponse.WaitAsync().ConfigureAwait(false);
                return AndroidBrokerHelper.InteractiveBrokerTokenResponse;
            }
        }

        private async Task AcquireTokenInteractiveViaContentProviderAsync(BrokerRequest brokerRequest)
        {
            using (_logger.LogMethodDuration())
            {
                _logger.Verbose("Starting Android Broker interactive authentication. ");

                Bundle bundleResult = await GetAcquireTokenInteractiveOperationBundleAsync().ConfigureAwait(false);

                var interactiveIntent = CreateInteractiveBrokerIntent(brokerRequest, bundleResult);

                _brokerHelper.LaunchInteractiveActivity(_parentActivity, interactiveIntent);
            }
        }

        private async Task<Bundle> GetAcquireTokenInteractiveOperationBundleAsync()
        {
            return await PerformContentResolverOperationAsync(ContentResolverOperation.acquireTokenInteractive, null).ConfigureAwait(false);
        }

        private Intent CreateInteractiveBrokerIntent(BrokerRequest brokerRequest, Bundle bundleResult)
        {
            string packageName = bundleResult.GetString("broker.package.name");
            string className = bundleResult.GetString("broker.activity.name");
            string uid = bundleResult.GetString("caller.info.uid");

            Intent brokerIntent = new Intent();
            brokerIntent.SetPackage(packageName);
            brokerIntent.SetClassName(
                    packageName,
                    className
            );

            brokerIntent.PutExtras(bundleResult);
            brokerIntent.PutExtra(BrokerConstants.NegotiatedBPVersionKey, _brokerHelper.NegotiatedBrokerProtocalKey);

            var interactiveIntent = brokerIntent.PutExtras(_brokerHelper.GetInteractiveBrokerBundle(brokerRequest));
            interactiveIntent.PutExtra(BrokerConstants.CallerInfoUID, Binder.CallingUid);

            return interactiveIntent;
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            _brokerHelper.CheckPowerOptimizationStatus();

            InitiateBrokerHandshakeAsync();

            BrokerRequest brokerRequest = BrokerRequest.FromSilentParameters(
                authenticationRequestParameters, acquireTokenSilentParameters);

            return await PerformAcquireTokenSilentAsync(brokerRequest).ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> PerformAcquireTokenSilentAsync(BrokerRequest brokerRequest)
        {
            try
            {
                return await AcquireTokenSilentViaBrokerAsync(brokerRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "Android broker silent invocation failed. ");
                _brokerHelper.HandleBrokerOperationError(ex);
                throw;
            }
        }

        private async Task<MsalTokenResponse> AcquireTokenSilentViaBrokerAsync(BrokerRequest brokerRequest)
        {
            using (_logger.LogMethodDuration())
            {
                _logger.Verbose("User is specified for silent token request. Starting silent Android broker request. ");

                brokerRequest = await UpdateRequestWithAccountInfoAsync(brokerRequest).ConfigureAwait(false);

                string silentResult = await PerformAcquireTokenSilentFromBrokerAsync(brokerRequest).ConfigureAwait(false);
                return _brokerHelper.HandleSilentAuthenticationResult(silentResult, brokerRequest.CorrelationId);
            }
        }

        /// <summary>
        /// This method is only used for Silent authentication requests so that we can check to see if an account exists in the account manager before
        /// sending the silent request to the broker. 
        /// </summary>
        public async Task<BrokerRequest> UpdateRequestWithAccountInfoAsync(BrokerRequest brokerRequest)
        {
            var accountData = await GetBrokerAccountDataAsync(brokerRequest).ConfigureAwait(false);

            return _brokerHelper.UpdateBrokerRequestWithAccountData(accountData, brokerRequest);
        }

        public async Task<string> PerformAcquireTokenSilentFromBrokerAsync(BrokerRequest brokerRequest)
        {
            Bundle silentOperationBundle = _brokerHelper.CreateSilentBrokerBundle(brokerRequest);
            var OperationBundleJSON = _brokerHelper.SearializeBundleToJSON(silentOperationBundle);
            var SilentOperationBundle = await PerformContentResolverOperationAsync(ContentResolverOperation.acquireTokenSilent, OperationBundleJSON).ConfigureAwait(false);
            return _brokerHelper.GetSilentResultFromBundle(SilentOperationBundle);
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

                return await PerformGetAccountsAsync(clientID, redirectUri).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<IAccount>> PerformGetAccountsAsync(string clientID, string redirectUri)
        {
            BrokerRequest brokerRequest = new BrokerRequest() { ClientId = clientID, RedirectUri = new Uri(redirectUri) };

            try
            {
                InitiateBrokerHandshakeAsync();
                var accountData = await GetBrokerAccountDataAsync(brokerRequest).ConfigureAwait(false);
                return _brokerHelper.ExtractBrokerAccountsFromAccountData(accountData);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to get Android broker accounts from the broker. ");
                _brokerHelper.HandleBrokerOperationError(ex);
                throw;
            }
        }

        private async Task<string> GetBrokerAccountDataAsync(BrokerRequest brokerRequest)
        {
            var getAccountsBundle = _brokerHelper.CreateBrokerAccountBundle(brokerRequest);
            var OperationBundleJSON = _brokerHelper.SearializeBundleToJSON(getAccountsBundle);
            var bundleResult = await PerformContentResolverOperationAsync(ContentResolverOperation.getAccounts, OperationBundleJSON).ConfigureAwait(false);

            return bundleResult?.GetString(BrokerConstants.BrokerAccounts);
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

                await PerformRemoveAccountsAsync(applicationConfiguration, account).ConfigureAwait(false);
            }
        }

        private async Task PerformRemoveAccountsAsync(IApplicationConfiguration applicationConfiguration, IAccount account)
        {
            try
            {
                InitiateBrokerHandshakeAsync();

                await RemoveBrokerAccountFromBrokersAsync(applicationConfiguration.ClientId, account).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to remove Android broker account from the broker. ");
                _brokerHelper.HandleBrokerOperationError(ex);
                throw;
            }
        }

        private async Task RemoveBrokerAccountFromBrokersAsync(string clientId, IAccount account)
        {
            var removeAccountsBundle = _brokerHelper.CreateRemoveBrokerAccountBundle(clientId, account);
            var OperationBundleJSON = _brokerHelper.SearializeBundleToJSON(removeAccountsBundle);
            await PerformContentResolverOperationAsync(ContentResolverOperation.removeAccounts, OperationBundleJSON).ConfigureAwait(false);
        }

        private async Task<Bundle> PerformContentResolverOperationAsync(ContentResolverOperation operation, string OperationParameters)
        {
            ContentResolver resolver = GetContentResolver();

            ICursor resultCursor = null;
            await Task.Run(() => resultCursor = resolver.Query(AndroidUri.Parse(GetContentProviderURIForOperation(Enum.GetName(typeof(ContentResolverOperation), operation))),
                                                            null,
                                                            OperationParameters,
                                                            null,
                                                            null)).ConfigureAwait(false);

            if (resultCursor == null)
            {
                _logger.Error("MSAL is unable to communicate to the broker.");
                throw new MsalClientException("broker_error");
            }

            var resultBundle = resultCursor.Extras;
            resultCursor.Close();

            return resultBundle;
        }

        private ContentResolver GetContentResolver()
        {
            if (_parentActivity == null)
            {
                return Application.Context.ContentResolver;
            }
            else
            {
                return _parentActivity.ContentResolver;
            }
        }

        public string GetContentProviderURIForOperation(string operation)
        {
            return "content://com.microsoft.windowsintune.companyportal.microsoft.identity.broker/" + operation;
        }

        public void HandleInstallUrl(string appLink)
        {
            _brokerHelper.HandleInstallUrl(appLink, _parentActivity);
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
