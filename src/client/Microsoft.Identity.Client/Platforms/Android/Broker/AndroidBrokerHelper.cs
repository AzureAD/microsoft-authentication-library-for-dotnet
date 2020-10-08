// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Content.PM;
using Android.Util;
using Java.Security;
using Java.Util.Concurrent;
using Signature = Android.Content.PM.Signature;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;
using OperationCanceledException = Android.Accounts.OperationCanceledException;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    [global::Android.Runtime.Preserve(AllMembers = true)]
    internal class AndroidBrokerHelper
    {
        private const string RedirectUriScheme = "msauth";
        private const string BrokerTag = BrokerConstants.Signature;
        public const string WorkAccount = "com.microsoft.workaccount.user.info";

        private readonly Context _androidContext;

        // Important: this object MUST be accessed on a background thread. Android will check this and throw otherwise.
        private readonly AccountManager _androidAccountManager;
        private readonly ICoreLogger _logger;

        private const long AccountManagerTimeoutSeconds = 5 * 60;

        public AndroidBrokerHelper(Context androidContext, ICoreLogger logger)
        {
            _androidContext = androidContext ?? throw new ArgumentNullException(nameof(androidContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.Verbose("Getting the Android context for broker request");
            _androidAccountManager = AccountManager.Get(_androidContext);
        }

        public bool CanSwitchToBroker()
        {
            string packageName = _androidContext.PackageName;

            // Rules are:
            // 1- broker app is installed
            // 2- signature of the broker is valid
            // 3- account exists

            //Force this to return true for broker test app

            return VerifyAuthenticator()
                   && !packageName.Equals(BrokerConstants.PackageName, StringComparison.OrdinalIgnoreCase)
                   && !packageName
           .Equals(BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase);
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

                result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                    BrokerConstants.AuthtokenType,
                    null,
                    addAccountOptions,
                    null,
                    null,
                    GetPreferredLooper(callerActivity));

                if (result == null)
                {
                    _logger.Info("Android account manager didn't return any results for interactive broker request.");
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
                    _logger.Error("An error occured when trying to communicate with account manager: " + ex.Message);
                }
                catch (Exception ex)
                {
                    throw new MsalClientException(MsalError.BrokerApplicationRequired, MsalErrorMessage.AndroidBrokerCannotBeInvoked, ex);
                }

                intent = (Intent)bundleResult?.GetParcelable(AccountManager.KeyIntent);

                //Validate that the intent was created successfully.
                if (intent != null)
                {
                    _logger.Info("Intent created from BundleResult is not null. Starting interactive broker request");
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
                _logger.Error("Error when trying to acquire intent for broker authentication.");
                throw;
            }

            return intent;
        }

        public async Task<string> GetBrokerAuthTokenSilentlyAsync(BrokerRequest brokerRequest, Activity callerActivity)
        {
            CheckForBrokerAccountInfoInAccountManager(brokerRequest, callerActivity);
            Bundle silentOperationBundle = CreateSilentBrokerBundle(brokerRequest);
            silentOperationBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.AcquireTokenSilent);

            IAccountManagerFuture result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                BrokerConstants.AuthtokenType,
                null,
                silentOperationBundle,
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
                _logger.Info($"Android Broker Silent call result - success? {success}.");

                if (!success)
                {
                    _logger.Warning($"Android Broker Silent call failed. " +
                        $"This usually means that the RT cannot be refreshed and interaction is required. " +
                        $"BundleResult: {bundleResult} Result string: {responseJson}");
                }

                // upstream logic knows how to extract potential errors from this result
                return responseJson;
            }

            _logger.Info("Android Broker didn't return any results.");
            return null;
        }

        /// <summary>
        /// This method is only used for Silent authnetication requests so that we can check to see if an account exists in the account manager before
        /// sending the silent request to the broker. 
        /// </summary>
        public void CheckForBrokerAccountInfoInAccountManager(BrokerRequest brokerRequest, Activity callerActivity)
        {
            var accounts = GetBrokerAccounts(brokerRequest, callerActivity);

            if (string.IsNullOrEmpty(accounts))
            {
                _logger.Info("Android account manager didn't return any accounts.");
                throw new MsalUiRequiredException(MsalError.NoAndroidBrokerAccountFound, MsalErrorMessage.NoAndroidBrokerAccountFound);
            }

            string username = brokerRequest.UserName;
            string homeAccountId = brokerRequest.HomeAccountId;
            string localAccountId = brokerRequest.LocalAccountId;

            if (!string.IsNullOrEmpty(accounts))
            {
                dynamic authResult = JArray.Parse(accounts);

                foreach (JObject account in authResult)
                {
                    var accountData = account[BrokerResponseConst.Account];

                    var accountDataHomeAccountID = accountData[BrokerResponseConst.HomeAccountId]?.ToString();
                    var accountDataLocalAccountID = accountData[BrokerResponseConst.LocalAccountId]?.ToString();

                    if (string.Equals(accountData[BrokerResponseConst.UserName].ToString(), username, StringComparison.OrdinalIgnoreCase))
                    {
                        // TODO: broker request should be immutable!
                        brokerRequest.HomeAccountId = accountDataHomeAccountID;
                        brokerRequest.LocalAccountId = accountDataLocalAccountID;
                        _logger.Info("Found broker account in Android account manager using the provided login hint.");
                        return;
                    }

                    if (string.Equals(accountDataHomeAccountID, homeAccountId, StringComparison.Ordinal) &&
                         string.Equals(accountDataLocalAccountID, localAccountId, StringComparison.Ordinal))
                    {
                        _logger.Info("Found broker account in Android account manager Using the provided account.");
                        return;
                    }
                }
            }

            _logger.Info("The requested account does not exist in the Android account manager.");
            throw new MsalUiRequiredException(MsalError.NoAndroidBrokerAccountFound, MsalErrorMessage.NoAndroidBrokerAccountFound);
        }

        private string GetBrokerAccounts(BrokerRequest brokerRequest, Activity callerActivity)
        {
            Bundle getAccountsBundle = CreateBrokerAccountBundle(brokerRequest);
            getAccountsBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.GetAccounts);

            //This operation will acquire all of the accounts in the account manager for the given client ID
            var result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                BrokerConstants.AuthtokenType,
                null,
                getAccountsBundle,
                null,
                null,
                GetPreferredLooper(callerActivity));

            Bundle bundleResult = (Bundle)result?.Result;
            return bundleResult?.GetString(BrokerConstants.BrokerAccounts);
        }

        /// <summary>
        /// This method will acquire all of the accounts in the account manager that have an access token for the given client ID.
        /// </summary>
        public IEnumerable<IAccount> GetBrokerAccountsInAccountManager(BrokerRequest brokerRequest)
        {
            var accounts = GetBrokerAccounts(brokerRequest, null);

            if (string.IsNullOrEmpty(accounts))
            {
                _logger.Info("Android account manager didn't return any accounts.");
            }

            List<IAccount> brokerAccounts = new List<IAccount>();

            if (!string.IsNullOrEmpty(accounts))
            {
                dynamic authResult = JArray.Parse(accounts);

                foreach (JObject account in authResult)
                {
                    if (account.ContainsKey(BrokerResponseConst.Account))
                    {
                        var accountData = account[BrokerResponseConst.Account];
                        var homeAccountID = accountData.Value<string>(BrokerResponseConst.HomeAccountId) ?? "";
                        var userName = accountData.Value<string>(BrokerResponseConst.UserName) ?? "";
                        var environment = accountData.Value<string>(BrokerResponseConst.Environment) ?? "";
                        IAccount iAccount = new Account(homeAccountID, userName, environment);
                        brokerAccounts.Add(iAccount);
                    }
                }
            }

            _logger.Info("Found " + brokerAccounts.Count + " accounts in the account manager.");

            return brokerAccounts;
        }

        public void RemoveBrokerAccountInAccountManager(string clientId, IAccount account)
        {
            Bundle removeAccountBundle = CreateRemoveBrokerAccountBundle(clientId, account);
            removeAccountBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.RemoveAccount);

            _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                BrokerConstants.AuthtokenType,
                null,
                removeAccountBundle,
                null,
                null,
                GetPreferredLooper(null));
        }

        //Inorder for broker to use the V2 endpoint during authentication, MSAL must initiate a handshake with broker to specify what endpoint should be used for the request.
        public async Task InitiateBrokerHandshakeAsync(Activity callerActivity)
        {
            using (_logger.LogMethodDuration())
            {
                try
                {
                    Bundle helloRequestBundle = new Bundle();
                    helloRequestBundle.PutString(BrokerConstants.ClientAdvertisedMaximumBPVersionKey, BrokerConstants.BrokerProtocalVersionCode);
                    helloRequestBundle.PutString(BrokerConstants.ClientConfiguredMinimumBPVersionKey, "2.0");
                    helloRequestBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, "HELLO");

                    IAccountManagerFuture result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
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
                        catch (OperationCanceledException ex)
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
                            errorDescription = "An error occurred during hand shake with the broker, no detailed error information was returned";
                        }

                        _logger.Error(errorDescription);
                        throw new MsalClientException(errorCode, errorDescription);
                    }

                    throw new MsalClientException("Could not communicate with broker via account manager. Please ensure power optimization settings are turned off.");
                }
                catch (Exception ex)
                {
                    _logger.Error("Error when trying to initiate communication with the broker.");
                    if (ex is MsalException)
                    {
                        throw;
                    }

                    throw new MsalClientException(MsalError.BrokerApplicationRequired, MsalErrorMessage.AndroidBrokerCannotBeInvoked, ex);
                }
            }
        }

        private Handler GetPreferredLooper(Activity callerActivity)
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

        private void ValidateBrokerRedirectURI(BrokerRequest brokerRequest)
        {
            using (_logger.LogMethodDuration())
            {
                string computedRedirectUri = GetRedirectUriForBroker();

                if (!string.Equals(computedRedirectUri, brokerRequest.RedirectUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, "The broker redirect URI is incorrect, it should be {0}. Please visit https://aka.ms/Brokered-Authentication-for-Android for more details.", computedRedirectUri);
                    _logger.Info(msg);
                    throw new MsalClientException(MsalError.CannotInvokeBroker, msg);
                }
            }
        }

        private string GetRedirectUriForBroker()
        {
            string packageName = Application.Context.PackageName;

            // First available signature. Applications can be signed with multiple
            // signatures.
            string signatureDigest = GetCurrentSignatureForPackage(packageName);
            if (!string.IsNullOrEmpty(signatureDigest))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}://{1}/{2}", RedirectUriScheme,
                    packageName.ToLowerInvariant(), signatureDigest);
            }

            return string.Empty;
        }

        private string GetCurrentSignatureForPackage(string packageName)
        {
            try
            {
                PackageInfo info = Application.Context.PackageManager.GetPackageInfo(packageName,
                    PackageInfoFlags.Signatures);
#pragma warning disable CS0618 // Type or member is obsolete - https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1854
                if (info != null && info.Signatures != null && info.Signatures.Count > 0)
                {
                    Signature signature = info.Signatures[0];
                    MessageDigest md = MessageDigest.GetInstance("SHA");
                    md.Update(signature.ToByteArray());
                    return Convert.ToBase64String(md.Digest(), Base64FormattingOptions.None);
                    // Server side needs to register all other tags. ADAL will
                    // send one of them.
                }
#pragma warning restore CS0618 // Type or member is obsolete

            }
            catch (PackageManager.NameNotFoundException)
            {
                _logger.Info("Calling App's package does not exist in PackageManager");
            }
            catch (NoSuchAlgorithmException)
            {
                _logger.Info("Digest SHA algorithm does not exists");
            }

            return null;
        }

        private Intent GetInteractiveBrokerIntent(BrokerRequest brokerRequest, Intent brokerIntent)
        {
            ValidateBrokerRedirectURI(brokerRequest);
            brokerIntent.PutExtra(BrokerConstants.BrokerRequestV2, JsonHelper.SerializeToJson(brokerRequest));

            return brokerIntent;
        }

        private Bundle CreateSilentBrokerBundle(BrokerRequest brokerRequest)
        {
            ValidateBrokerRedirectURI(brokerRequest);
            Bundle bundle = new Bundle();
            bundle.PutString(BrokerConstants.BrokerRequestV2, JsonHelper.SerializeToJson(brokerRequest));
            bundle.PutInt(BrokerConstants.CallerInfoUID, Binder.CallingUid);

            return bundle;
        }


        private Bundle CreateBrokerAccountBundle(BrokerRequest brokerRequest)
        {
            Bundle bundle = new Bundle();

            bundle.PutString(BrokerConstants.AccountClientIdKey, brokerRequest.ClientId);
            bundle.PutString(BrokerConstants.AccountRedirect, brokerRequest.UrlEncodedRedirectUri);
            bundle.PutInt(BrokerConstants.CallerInfoUID, Binder.CallingUid);

            return bundle;
        }

        private Bundle CreateRemoveBrokerAccountBundle(string clientId, IAccount account)
        {
            Bundle bundle = new Bundle();

            bundle.PutString(BrokerConstants.AccountClientIdKey, clientId);
            bundle.PutString(BrokerConstants.Environment, account.Environment);
            bundle.PutString(BrokerConstants.HomeAccountIDKey, account.HomeAccountId.Identifier);

            return bundle;
        }

        private bool VerifySignature(string brokerPackageName)
        {
            List<X509Certificate2> certs = ReadCertDataForBrokerApp(brokerPackageName);

            VerifySignatureHash(certs);
            if (certs.Count > 1)
            {
                // Verify the certificate chain is chained correctly.
                VerifyCertificateChain(certs);
            }

            return true;
        }

        private void VerifySignatureHash(List<X509Certificate2> certs)
        {
            bool validSignatureFound = false;

            foreach (var signerCert in certs)
            {
                MessageDigest messageDigest = MessageDigest.GetInstance("SHA");
                messageDigest.Update(signerCert.RawData);

                // Check the hash for signer cert is the same as what we hardcoded.
                string signatureHash = Base64.EncodeToString(messageDigest.Digest(), Base64Flags.NoWrap);
                if (BrokerTag.Equals(signatureHash, StringComparison.OrdinalIgnoreCase) ||
                    BrokerConstants.AzureAuthenticatorAppSignature.Equals(signatureHash, StringComparison.OrdinalIgnoreCase))
                {
                    validSignatureFound = true;
                }
            }

            if (!validSignatureFound)
            {
                throw new MsalClientException(MsalError.AndroidBrokerSignatureVerificationFailed, "No matching signature found");
            }
        }

        private void VerifyCertificateChain(List<X509Certificate2> certificates)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection(certificates.ToArray());
            X509Chain chain = new X509Chain();
            chain.ChainPolicy = new X509ChainPolicy()
            {
#pragma warning disable IA5352 //Certificates are not published to a CRL when revoked for broker so this check cannot be made
                RevocationMode = X509RevocationMode.NoCheck
#pragma warning restore IA5352
            };

            chain.ChainPolicy.ExtraStore.AddRange(collection);
            foreach (X509Certificate2 certificate in certificates)
            {
                var chainBuilt = chain.Build(certificate);

                if (!chainBuilt)
                {
                    foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                    {
                        if (chainStatus.Status != X509ChainStatusFlags.UntrustedRoot)
                        {
                            throw new MsalClientException(MsalError.AndroidBrokerSignatureVerificationFailed,
                                string.Format(CultureInfo.InvariantCulture, "app certificate validation failed with {0}", chainStatus.Status));
                        }
                    }
                }
            }

        }

        private List<X509Certificate2> ReadCertDataForBrokerApp(string brokerPackageName)
        {
            PackageInfo packageInfo = _androidContext.PackageManager.GetPackageInfo(brokerPackageName,
                PackageInfoFlags.Signatures);
            if (packageInfo == null)
            {
                throw new MsalClientException(MsalError.AndroidBrokerSignatureVerificationFailed, "No broker package found");
            }
#pragma warning disable CS0618 // Type or member is obsolete https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1854

            if (packageInfo.Signatures == null || packageInfo.Signatures.Count == 0)
            {
                throw new MsalClientException(MsalError.AndroidBrokerSignatureVerificationFailed, "No signature associated with the broker package.");
            }

            List<X509Certificate2> certificates = new List<X509Certificate2>(packageInfo.Signatures.Count);
            foreach (Signature signature in packageInfo.Signatures)
            {
                byte[] rawCert = signature.ToByteArray();
                X509Certificate2 x509Certificate = null;
                x509Certificate = new X509Certificate2(rawCert);
                certificates.Add(x509Certificate);
            }
#pragma warning restore CS0618 // Type or member is obsolete

            return certificates;
        }

        private bool VerifyAuthenticator()
        {
            using (_logger.LogMethodDuration())
            {
                // there may be multiple authenticators from same package
                // , but there is only one entry for an authenticator type in
                // AccountManager.
                // If another app tries to install same authenticator type, it will
                // queue up and will be active after first one is uninstalled.
                AuthenticatorDescription[] authenticators = _androidAccountManager.GetAuthenticatorTypes();
                foreach (AuthenticatorDescription authenticator in authenticators)
                {
                    if (authenticator.Type.Equals(BrokerConstants.BrokerAccountType, StringComparison.OrdinalIgnoreCase)
                        && VerifySignature(authenticator.PackageName))
                    {
                        _logger.Verbose("Found the Authenticator on the device");
                        return true;
                    }
                }

                _logger.Warning("No Authenticator found on the device.");
                return false;
            }
        }

        public static string GetValueFromBrokerPayload(IDictionary<string, string> brokerPayload, string key)
        {
            string value;
            if (brokerPayload.TryGetValue(key, out value))
            {
                return value;
            }

            return string.Empty;
        }
    }
}
