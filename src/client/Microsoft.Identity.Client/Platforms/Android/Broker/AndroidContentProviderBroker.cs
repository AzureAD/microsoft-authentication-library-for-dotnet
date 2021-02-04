// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
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
        private string _negotiatedBrokerProtocolKey = String.Empty;

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

        public async void InitiateBrokerHandShakeAsync()
        {
            using (_logger.LogMethodDuration())
            {
                Bundle handShakeBundleResult = await GetHandShakeBundleResultFromBrokerAsync().ConfigureAwait(false);

                _negotiatedBrokerProtocolKey = GetProtocolKeyFromHandShakeResult(handShakeBundleResult);
            }
        }

        private async Task<Bundle> GetHandShakeBundleResultFromBrokerAsync()
        {
            var bundle = _brokerHelper.CreateHandShakeOperationBundle();
            var operationBundleJSON = SearializeBundleToJSON(bundle);
            return await PerformContentResolverOperationAsync(ContentResolverOperation.hello, operationBundleJSON).ConfigureAwait(false);
        }

        public string GetProtocolKeyFromHandShakeResult(Bundle bundleResult)
        {
            var negotiatedBrokerProtocalKey = bundleResult?.GetString(BrokerConstants.NegotiatedBPVersionKey);

            if (!string.IsNullOrEmpty(negotiatedBrokerProtocalKey))
            {
                _logger.Info("Using broker protocol version: " + negotiatedBrokerProtocalKey);
                return negotiatedBrokerProtocalKey;
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

        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            InitiateBrokerHandShakeAsync();

            BrokerRequest brokerRequest = BrokerRequest.FromInteractiveParameters(authenticationRequestParameters, acquireTokenInteractiveParameters);

            // There can only be 1 broker request at a time so keep track of the correlation id
            AndroidBrokerStaticHelper.InteractiveRequestCorrelationId = brokerRequest.CorrelationId;

            return await AcquireTokenInteractiveInternalAsync(brokerRequest).ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> AcquireTokenInteractiveInternalAsync(BrokerRequest brokerRequest)
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
                await AndroidBrokerStaticHelper.ReadyForResponse.WaitAsync().ConfigureAwait(false);
                return AndroidBrokerStaticHelper.InteractiveBrokerTokenResponse;
            }
        }

        private async Task AcquireTokenInteractiveViaContentProviderAsync(BrokerRequest brokerRequest)
        {
            using (_logger.LogMethodDuration())
            {
                _logger.Verbose("Starting Android Broker interactive authentication. ");

                Bundle bundleResult = await PerformContentResolverOperationAsync(ContentResolverOperation.acquireTokenInteractive, null).ConfigureAwait(false);

                var interactiveIntent = CreateInteractiveBrokerIntent(brokerRequest, bundleResult);

                _brokerHelper.LaunchInteractiveActivity(_parentActivity, interactiveIntent);
            }
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
            brokerIntent.PutExtra(BrokerConstants.NegotiatedBPVersionKey, _negotiatedBrokerProtocolKey);

            var interactiveIntent = brokerIntent.PutExtras(CreateInteractiveBrokerBundle(brokerRequest));
            interactiveIntent.PutExtra(BrokerConstants.CallerInfoUID, Binder.CallingUid);

            return interactiveIntent;
        }

        private Bundle CreateInteractiveBrokerBundle(BrokerRequest brokerRequest)
        {
            _brokerHelper.ValidateBrokerRedirectURI(brokerRequest);

            Bundle bundle = new Bundle();
            string brokerRequestJson = JsonHelper.SerializeToJson(brokerRequest);
            bundle.PutString(BrokerConstants.BrokerRequestV2, brokerRequestJson);
            bundle.PutInt(BrokerConstants.CallerInfoUID, Binder.CallingUid);
            _logger.InfoPii("GetInteractiveBrokerBundle: " + brokerRequestJson, "Enable PII to see the broker request. ");
            return bundle;
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            InitiateBrokerHandShakeAsync();

            BrokerRequest brokerRequest = BrokerRequest.FromSilentParameters(
                authenticationRequestParameters, acquireTokenSilentParameters);

            return await AcquireTokenSilentInternalAsync(brokerRequest).ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> AcquireTokenSilentInternalAsync(BrokerRequest brokerRequest)
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

                var accountData = await GetBrokerAccountDataAsync(brokerRequest).ConfigureAwait(false);

                brokerRequest = _brokerHelper.UpdateBrokerRequestWithAccountData(accountData, brokerRequest);

                string silentResult = await AcquireTokenSilentFromBrokerInternalAsync(brokerRequest).ConfigureAwait(false);
                return _brokerHelper.HandleSilentAuthenticationResult(silentResult, brokerRequest.CorrelationId);
            }
        }

        private async Task<string> AcquireTokenSilentFromBrokerInternalAsync(BrokerRequest brokerRequest)
        {
            Bundle silentOperationBundle = _brokerHelper.CreateSilentBrokerBundle(brokerRequest);
            var operationBundleJSON = SearializeBundleToJSON(silentOperationBundle);
            var silentOperationBundleResult = await PerformContentResolverOperationAsync(ContentResolverOperation.acquireTokenSilent, operationBundleJSON).ConfigureAwait(false);
            
            if (silentOperationBundleResult != null)
            {
                return _brokerHelper.GetSilentResultFromBundle(silentOperationBundleResult);
            }

            _logger.Info("Android Broker didn't return any results. ");
            return null;
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync(string clientID, string redirectUri)
        {
            using (_logger.LogMethodDuration())
            {
                if (!IsBrokerInstalledAndInvokable())
                {
                    _logger.Warning("Android broker is either not installed or is not reachable so no accounts will be returned. ");
                    return Enumerable.Empty<IAccount>();
                }

                return await GetAccountsInternalAsync(clientID, redirectUri).ConfigureAwait(false);
            }
        }

        private async Task<IEnumerable<IAccount>> GetAccountsInternalAsync(string clientID, string redirectUri)
        {
            BrokerRequest brokerRequest = new BrokerRequest() { ClientId = clientID, RedirectUri = new Uri(redirectUri) };

            try
            {
                InitiateBrokerHandShakeAsync();
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
            var operationBundleJSON = SearializeBundleToJSON(getAccountsBundle);
            var bundleResult = await PerformContentResolverOperationAsync(ContentResolverOperation.getAccounts, operationBundleJSON).ConfigureAwait(false);

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
                InitiateBrokerHandShakeAsync();

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
            var operationBundleJSON = SearializeBundleToJSON(removeAccountsBundle);
            await PerformContentResolverOperationAsync(ContentResolverOperation.removeAccounts, operationBundleJSON).ConfigureAwait(false);
        }

        private async Task<Bundle> PerformContentResolverOperationAsync(ContentResolverOperation operation, string OperationParameters)
        {
            ContentResolver resolver = GetContentResolver();

            ICursor resultCursor = null;
            await Task.Run(() => resultCursor = resolver.Query(AndroidUri.Parse(GetContentProviderURIForOperation(Enum.GetName(typeof(ContentResolverOperation), operation))),
                                                            !string.IsNullOrEmpty(_negotiatedBrokerProtocolKey) ? new string[] { _negotiatedBrokerProtocolKey } : null,
                                                            OperationParameters,
                                                            null,
                                                            null)).ConfigureAwait(false);

            if (resultCursor == null)
            {
                _logger.Error("An error occurred during the content provider operation.");
                throw new MsalClientException(MsalError.CannotInvokeBroker, "Could not communicate with broker via content provider.");
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

        private string GetContentProviderURIForOperation(string operation)
        {
            //We need to check which authenticator is currently active so that we can properly construct the content provider Uri
            if (_brokerHelper.Authenticator.PackageName.Contains(BrokerConstants.CompanyPortalPackageName))
            {
                return BrokerConstants.CompanyPortalContentProviderUri + "/" + operation;
            }

            return BrokerConstants.MsAuthenticatorContentProviderUri + "/" + operation;
        }

        private string SearializeBundleToJSON(Bundle bundle)
        {
            return Base64UrlHelpers.Encode(marshall(bundle));
        }

        private static byte[] marshall(Bundle parcelable)
        {
            Parcel parcel = Parcel.Obtain();
            parcel.WriteBundle(parcelable);

            return parcel.Marshall();
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
