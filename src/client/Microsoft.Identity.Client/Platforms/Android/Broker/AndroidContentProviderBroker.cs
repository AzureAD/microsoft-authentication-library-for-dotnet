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
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
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
#if MAUI
    [Preserve(AllMembers = true)]
#else
    [AndroidNative.Runtime.Preserve(AllMembers = true)]
#endif
    internal class AndroidContentProviderBroker : IBroker
    {
        private readonly AndroidBrokerHelper _brokerHelper;
        private readonly ILoggerAdapter _logger;
        private readonly Activity _parentActivity;
        private string _negotiatedBrokerProtocolKey = string.Empty;

        public bool IsPopSupported => false;

        public AndroidContentProviderBroker(CoreUIParent uiParent, ILoggerAdapter logger)
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

        public async Task InitiateBrokerHandShakeAsync()
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
            var serializedOperationBundle = SerializeBundle(bundle);
            return await PerformContentResolverOperationAsync(ContentResolverOperation.hello, serializedOperationBundle).ConfigureAwait(false);
        }

        public string GetProtocolKeyFromHandShakeResult(Bundle bundleResult)
        {
            var negotiatedBrokerProtocalKey = bundleResult?.GetString(BrokerConstants.NegotiatedBPVersionKey);

            if (!string.IsNullOrEmpty(negotiatedBrokerProtocalKey))
            {
                _logger.Info(() => "[Android broker] Using broker protocol version: " + negotiatedBrokerProtocalKey);
                return negotiatedBrokerProtocalKey;
            }

            dynamic errorResult = JObject.Parse(bundleResult?.GetString(BrokerConstants.BrokerResultV2));
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

        public async Task<MsalTokenResponse> AcquireTokenInteractiveAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters acquireTokenInteractiveParameters)
        {
            await InitiateBrokerHandShakeAsync().ConfigureAwait(false);

            BrokerRequest brokerRequest = BrokerRequest.FromInteractiveParameters(authenticationRequestParameters, acquireTokenInteractiveParameters);

            // There can only be 1 broker request at a time so keep track of the correlation id
            AndroidBrokerInteractiveResponseHelper.InteractiveRequestCorrelationId = brokerRequest.CorrelationId;

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
                _logger.ErrorPiiWithPrefix(ex, "[Android broker] Interactive invocation failed. ");
                _brokerHelper.HandleBrokerOperationError(ex);
            }

            using (_logger.LogBlockDuration("[Android broker] Waiting for broker response. "))
            {
                await AndroidBrokerInteractiveResponseHelper.ReadyForResponse.WaitAsync().ConfigureAwait(false);
                return AndroidBrokerInteractiveResponseHelper.InteractiveBrokerTokenResponse;
            }
        }

        private async Task AcquireTokenInteractiveViaContentProviderAsync(BrokerRequest brokerRequest)
        {
            using (_logger.LogMethodDuration())
            {
                _logger.Verbose(()=>"[Android broker] Starting interactive authentication. ");

                Bundle bundleResult = await PerformContentResolverOperationAsync(ContentResolverOperation.acquireTokenInteractive, null).ConfigureAwait(false);

                var interactiveIntent = CreateInteractiveBrokerIntent(brokerRequest, bundleResult);

                _brokerHelper.LaunchInteractiveActivity(_parentActivity, interactiveIntent);
            }
        }

        private Intent CreateInteractiveBrokerIntent(BrokerRequest brokerRequest, Bundle bundleResult)
        {
            string packageName = bundleResult.GetString("broker.package.name");
            string className = bundleResult.GetString("broker.activity.name");

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
            _brokerHelper.ValidateBrokerRedirectUri(brokerRequest);

            Bundle bundle = new Bundle();
            string brokerRequestJson = JsonHelper.SerializeToJson(brokerRequest);
            bundle.PutString(BrokerConstants.BrokerRequestV2, brokerRequestJson);
            bundle.PutInt(BrokerConstants.CallerInfoUID, Binder.CallingUid);
            _logger.InfoPii(
                () => "[Android broker] GetInteractiveBrokerBundle: " + brokerRequestJson, 
                () => "Enable PII to see the broker request. ");
            return bundle;
        }

        public async Task<MsalTokenResponse> AcquireTokenSilentAsync(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters)
        {
            await InitiateBrokerHandShakeAsync().ConfigureAwait(false);

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
                _logger.ErrorPiiWithPrefix(ex, "[Android broker] Silent invocation failed. ");
                _brokerHelper.HandleBrokerOperationError(ex);
                throw;
            }
        }

        private async Task<MsalTokenResponse> AcquireTokenSilentViaBrokerAsync(BrokerRequest brokerRequest)
        {
            using (_logger.LogMethodDuration())
            {
                _logger.Verbose(()=>"[Android broker] User is specified for silent token request. Starting silent request. ");

                var accountData = await GetBrokerAccountDataAsync(brokerRequest).ConfigureAwait(false);

                brokerRequest = _brokerHelper.UpdateBrokerRequestWithAccountData(accountData, brokerRequest);

                string silentResult = await AcquireTokenSilentFromBrokerInternalAsync(brokerRequest).ConfigureAwait(false);
                return _brokerHelper.HandleSilentAuthenticationResult(silentResult, brokerRequest.CorrelationId);
            }
        }

        private async Task<string> AcquireTokenSilentFromBrokerInternalAsync(BrokerRequest brokerRequest)
        {
            Bundle silentOperationBundle = _brokerHelper.CreateSilentBrokerBundle(brokerRequest);
            var serializedOperationBundle = SerializeBundle(silentOperationBundle);
            var silentOperationBundleResult = await PerformContentResolverOperationAsync(ContentResolverOperation.acquireTokenSilent, serializedOperationBundle).ConfigureAwait(false);
            
            if (silentOperationBundleResult != null)
            {
                return _brokerHelper.GetSilentResultFromBundle(silentOperationBundleResult);
            }

            _logger.Info("[Android broker] No results returned. ");
            return null;
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
                    return null;
                }

                return await GetAccountsInternalAsync(clientId, redirectUri).ConfigureAwait(false);
            }
        }

        private async Task<IReadOnlyList<IAccount>> GetAccountsInternalAsync(string clientID, string redirectUri)
        {
            BrokerRequest brokerRequest = new BrokerRequest() { ClientId = clientID, RedirectUri = new Uri(redirectUri) };

            try
            {
                await InitiateBrokerHandShakeAsync().ConfigureAwait(false);
                var accountData = await GetBrokerAccountDataAsync(brokerRequest).ConfigureAwait(false);
                return _brokerHelper.ExtractBrokerAccountsFromAccountData(accountData);
            }
            catch (Exception ex)
            {
                _logger.Error("[Android broker] Failed to get accounts from the broker. ");
                _brokerHelper.HandleBrokerOperationError(ex);
                throw;
            }
        }

        private async Task<string> GetBrokerAccountDataAsync(BrokerRequest brokerRequest)
        {
            var getAccountsBundle = _brokerHelper.CreateBrokerAccountBundle(brokerRequest);
            var serializedOperationBundle = SerializeBundle(getAccountsBundle);
            var bundleResult = await PerformContentResolverOperationAsync(ContentResolverOperation.getAccounts, serializedOperationBundle).ConfigureAwait(false);

            return bundleResult?.GetString(BrokerConstants.BrokerAccounts);
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
                    await InitiateBrokerHandShakeAsync().ConfigureAwait(false);

                    await RemoveBrokerAccountFromBrokersAsync(appConfig.ClientId, account).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error("[Android broker] Failed to remove account from the broker. ");
                    _brokerHelper.HandleBrokerOperationError(ex);
                    throw;
                }
            }
        }

        private async Task RemoveBrokerAccountFromBrokersAsync(string clientId, IAccount account)
        {
            var removeAccountsBundle = _brokerHelper.CreateRemoveBrokerAccountBundle(clientId, account);
            var serializedOperationBundle = SerializeBundle(removeAccountsBundle);
            await PerformContentResolverOperationAsync(ContentResolverOperation.removeAccounts, serializedOperationBundle).ConfigureAwait(false);
        }

        private async Task<Bundle> PerformContentResolverOperationAsync(ContentResolverOperation operation, string operationParameters)
        {
            ContentResolver resolver = GetContentResolver();

            var contentResolverUri = GetContentProviderUriForOperation(Enum.GetName(typeof(ContentResolverOperation), operation));
            _logger.Info(() => $"[Android broker] Executing content resolver operation: {operation} URI: {contentResolverUri}");

            ICursor resultCursor = null;

            await Task.Run(() => resultCursor = resolver.Query(AndroidUri.Parse(contentResolverUri),
                                                            !string.IsNullOrEmpty(_negotiatedBrokerProtocolKey) ? new string[] { _negotiatedBrokerProtocolKey } : null,
                                                            operationParameters,
                                                            null,
                                                            null)).ConfigureAwait(false);

            if (resultCursor == null)
            {
                _logger.Error($"[Android broker] An error occurred during the content provider operation {operation}.");
                throw new MsalClientException(MsalError.CannotInvokeBroker, "[Android broker] Could not communicate with broker via content provider."
                    + $"Operation: {operation} URI: {contentResolverUri}");
            }

            var resultBundle = resultCursor.Extras;

            resultCursor.Close();
            resultCursor.Dispose();

            if (resultBundle != null)
            {
                _logger.Verbose(()=>$"[Android broker] Content resolver operation completed successfully. Operation: {operation} URI: {contentResolverUri}");
            }

            return resultBundle;
        }

        private ContentResolver GetContentResolver()
        {
            return _parentActivity?.ContentResolver ?? Application.Context.ContentResolver;
        }

        private string GetContentProviderUriForOperation(string operation)
        {
            //We need to check which authenticator is currently active so that we can properly construct the content provider Uri
            if (_brokerHelper.Authenticator.PackageName.Contains(BrokerConstants.CompanyPortalPackageName))
            {
                return BrokerConstants.CompanyPortalContentProviderUri + "/" + operation;
            }

            return BrokerConstants.MsAuthenticatorContentProviderUri + "/" + operation;
        }

        private string SerializeBundle(Bundle bundle)
        {
            return Convert.ToBase64String(Marshall(bundle));
        }

        private static byte[] Marshall(Bundle parcelable)
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

        public Task<MsalTokenResponse> AcquireTokenByUsernamePasswordAsync(AuthenticationRequestParameters authenticationRequestParameters, AcquireTokenByUsernamePasswordParameters acquireTokenByUsernamePasswordParameters)
        {
            return Task.FromResult<MsalTokenResponse>(null); // nop
        }
    }
}
