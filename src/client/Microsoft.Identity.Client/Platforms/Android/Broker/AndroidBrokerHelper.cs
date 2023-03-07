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
using OperationCanceledException = Android.Accounts.OperationCanceledException;
using AndroidUri = Android.Net.Uri;
using Android.Database;
using Microsoft.Identity.Json.Utilities;
using System.Threading;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Http;
using AndroidNative = Android;
using System.Linq;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
#if MAUI
    [Preserve(AllMembers = true)]
#else
    [global::Android.Runtime.Preserve(AllMembers = true)]
#endif
    internal class AndroidBrokerHelper
    {
        private const string RedirectUriScheme = "msauth";
        private const string BrokerTag = BrokerConstants.Signature;

        private readonly Context _androidContext;

        // Important: this object MUST be accessed on a background thread. Android will check this and throw otherwise.
        public AccountManager AndroidAccountManager { get; }
        private readonly ILoggerAdapter _logger;

        public AndroidBrokerHelper(Context androidContext, ILoggerAdapter logger)
        {
            _androidContext = androidContext ?? throw new ArgumentNullException(nameof(androidContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.Verbose(()=>"[Android broker] Getting the Android context for broker request. ");
            AndroidAccountManager = AccountManager.Get(_androidContext);
        }

        public bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
            using (_logger.LogMethodDuration())
            {
                bool canInvoke = CanSwitchToBroker();
                _logger.Verbose(()=>"[Android broker] Can invoke broker? " + canInvoke);

                return canInvoke;
            }
        }

        private bool CanSwitchToBroker()
        {
            string packageName = _androidContext.PackageName;

            // Rules are:
            // 1- broker app is installed
            // 2- signature of the broker is valid
            // 3- account exists

            //Force this to return true for broker test app
            var authenticator = GetInstalledAuthenticator();
            return authenticator!= null
                   && !packageName.Equals(BrokerConstants.PackageName, StringComparison.OrdinalIgnoreCase)
                   && !packageName
           .Equals(BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase);
        }

        public Bundle CreateHandShakeOperationBundle()
        {
            Bundle handshakeOperationBundle = new Bundle();
            handshakeOperationBundle.PutString(BrokerConstants.ClientAdvertisedMaximumBPVersionKey, BrokerConstants.BrokerProtocolVersionCode);
            handshakeOperationBundle.PutString(BrokerConstants.ClientConfiguredMinimumBPVersionKey, "2.0");
            handshakeOperationBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, "HELLO");

            return handshakeOperationBundle;
        }

        public string GetSilentResultFromBundle(Bundle bundleResult)
        {
            string responseJson = bundleResult.GetString(BrokerConstants.BrokerResultV2);

            bool success = bundleResult.GetBoolean(BrokerConstants.BrokerRequestV2Success);
            _logger.Info(() => $"[Android broker] Silent call result - success? {success}. ");

            if (!success)
            {
                _logger.Warning($"[Android broker] Silent call failed. " +
                    $"This usually means that the RT cannot be refreshed and interaction is required. " +
                    $"BundleResult: {bundleResult} Result string: {responseJson}");
            }

            // upstream logic knows how to extract potential errors from this result
            return responseJson;
        }

        public BrokerRequest UpdateBrokerRequestWithAccountData(string accountData, BrokerRequest brokerRequest)
        {
            if (string.IsNullOrEmpty(accountData))
            {
                _logger.Info("[Android broker] Android account manager didn't return any accounts. ");
                throw new MsalUiRequiredException(MsalError.NoAndroidBrokerAccountFound, MsalErrorMessage.NoAndroidBrokerAccountFound);
            }

            string username = brokerRequest.UserName;
            string homeAccountId = brokerRequest.HomeAccountId;
            string localAccountId = brokerRequest.LocalAccountId;

                dynamic AccountDataList = JArray.Parse(accountData);

                foreach (JObject account in AccountDataList)
                {
                    var accountInfo = account[BrokerResponseConst.Account];
                    var accountInfoHomeAccountID = accountInfo[BrokerResponseConst.HomeAccountId]?.ToString();
                    var accountInfoLocalAccountID = accountInfo[BrokerResponseConst.LocalAccountId]?.ToString();

                    if (string.Equals(accountInfo[BrokerResponseConst.UserName].ToString(), username, StringComparison.OrdinalIgnoreCase))
                    {
                        // TODO: broker request should be immutable!
                        brokerRequest.HomeAccountId = accountInfoHomeAccountID;
                        brokerRequest.LocalAccountId = accountInfoLocalAccountID;
                        _logger.Info("[Android broker] Found broker account in Android account manager using the provided login hint. ");
                        return brokerRequest;
                    }

                    if (string.Equals(accountInfoHomeAccountID, homeAccountId, StringComparison.Ordinal) &&
                         string.Equals(accountInfoLocalAccountID, localAccountId, StringComparison.Ordinal))
                    {
                        _logger.Info("[Android broker] Found broker account in Android account manager using the provided account. ");
                        return brokerRequest;
                    }
                }

            _logger.Info("[Android broker] The requested account does not exist in the Android account manager. ");
            throw new MsalUiRequiredException(MsalError.NoAndroidBrokerAccountFound, MsalErrorMessage.NoAndroidBrokerAccountFound);
        }

        /// <summary>
        /// This method will acquire all of the accounts in the account manager that have an access token for the given client ID.
        /// </summary>
        public IReadOnlyList<IAccount> ExtractBrokerAccountsFromAccountData(string accountData)
        {
            List<IAccount> brokerAccounts = new List<IAccount>();

            if (!string.IsNullOrEmpty(accountData))
            {
                dynamic authResult = JArray.Parse(accountData);

                foreach (JObject account in authResult)
                {
                    if (account.ContainsKey(BrokerResponseConst.Account))
                    {
                        var accountInfo = account[BrokerResponseConst.Account];
                        IAccount iAccount = new Account(
                            accountInfo.Value<string>(BrokerResponseConst.HomeAccountId) ?? string.Empty,
                            accountInfo.Value<string>(BrokerResponseConst.UserName) ?? string.Empty,
                            accountInfo.Value<string>(BrokerResponseConst.Environment) ?? string.Empty);
                        brokerAccounts.Add(iAccount);
                    }
                }
            }

            _logger.Info(() => "[Android broker] Found " + brokerAccounts.Count + " accounts in the account manager. ");

            return brokerAccounts;
        }

        public void ValidateBrokerRedirectUri(BrokerRequest brokerRequest)
        {
            using (_logger.LogMethodDuration())
            {
                string computedRedirectUri = GetRedirectUriForBroker();

                if (!string.Equals(computedRedirectUri, brokerRequest.RedirectUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, "[Android broker] The broker redirect URI is incorrect, it should be {0}. Please visit https://aka.ms/Brokered-Authentication-for-Android for more details. ", computedRedirectUri);
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
                _logger.Info("[Android broker] Calling App's package does not exist in PackageManager. ");
            }
            catch (NoSuchAlgorithmException)
            {
                _logger.Info("[Android broker] Digest SHA algorithm does not exists. ");
            }

            return null;
        }

        public Bundle CreateSilentBrokerBundle(BrokerRequest brokerRequest)
        {
            ValidateBrokerRedirectUri(brokerRequest);
            Bundle bundle = new Bundle();
            string brokerRequestJson = JsonHelper.SerializeToJson(brokerRequest);
            _logger.InfoPii(() => "[Android broker] CreateSilentBrokerBundle: " + brokerRequestJson, () => "Enable PII to see the silent broker request. ");
            bundle.PutString(BrokerConstants.BrokerRequestV2, brokerRequestJson);
            bundle.PutInt(BrokerConstants.CallerInfoUID, Binder.CallingUid);

            return bundle;
        }

        public Bundle CreateBrokerAccountBundle(BrokerRequest brokerRequest)
        {
            _logger.InfoPii(
                () => "[Android broker] CreateBrokerAccountBundle: " + JsonHelper.SerializeToJson(brokerRequest), 
                () => "Enable PII to see the broker account bundle request. ");
            Bundle bundle = new Bundle();

            bundle.PutString(BrokerConstants.AccountClientIdKey, brokerRequest.ClientId);
            bundle.PutString(BrokerConstants.AccountRedirect, brokerRequest.UrlEncodedRedirectUri);
            bundle.PutInt(BrokerConstants.CallerInfoUID, Binder.CallingUid);

            return bundle;
        }

        public Bundle CreateRemoveBrokerAccountBundle(string clientId, IAccount account)
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

        private AuthenticatorDescription GetInstalledAuthenticator()
        {
            using (_logger.LogMethodDuration())
            {
                foreach (AuthenticatorDescription authenticator in AndroidAccountManager.GetAuthenticatorTypes())
                {
                    if (authenticator.Type.Equals(BrokerConstants.BrokerAccountType, StringComparison.OrdinalIgnoreCase)
                        && VerifySignature(authenticator.PackageName))
                    {
                        _logger.Verbose(()=>"[Android broker] Found the Authenticator on the device. ");
                        return authenticator;
                    }
                }

                _logger.Warning("[Android broker] No Authenticator found on the device. ");
                return null;
            }
        }

        // There may be multiple authenticators from same package
        // , but there is only one entry for an authenticator type in
        // AccountManager.
        // If another app tries to install same authenticator type, it will
        // queue up and will be active after first one is uninstalled.
        public AuthenticatorDescription Authenticator
        {
            get
            {
                return GetInstalledAuthenticator();
            }
        }

        public void LaunchInteractiveActivity(Activity activity, Intent interactiveIntent)
        {
            // onActivityResult will receive the response for this activity.
            // Launching this activity will switch to the broker app.
            try
            {
                _logger.Info(
                    () => "[Android broker] Calling activity pid:" + Process.MyPid()
                    + " tid:" + Process.MyTid() + "uid:"
                    + Process.MyUid());

                activity.StartActivityForResult(interactiveIntent, 1001);
            }
            catch (ActivityNotFoundException e)
            {
                _logger.ErrorPiiWithPrefix(e, "[Android broker] Unable to get Android activity during interactive broker request. ");
                throw;
            }
        }

        public MsalTokenResponse HandleSilentAuthenticationResult(string silentResult, string correlationId)
        {
            if (!string.IsNullOrEmpty(silentResult))
            {
                return MsalTokenResponse.CreateFromAndroidBrokerResponse(silentResult, correlationId);
            }

            return new MsalTokenResponse
            {
                Error = MsalError.BrokerResponseReturnedError,
                ErrorDescription = "[Android broker] Unknown broker error. Failed to acquire token silently from the broker. " + MsalErrorMessage.AndroidBrokerCannotBeInvoked,
            };
        }

        public void HandleBrokerOperationError(Exception ex)
        {
            _logger.Error(ex.Message);
            if (ex is MsalException)
                throw ex;
            else
                throw new MsalClientException(MsalError.AndroidBrokerOperationFailed, ex.Message, ex);
        }

        public void HandleInstallUrl(string appLink, Activity activity)
        {
            _logger.Info(() => "[Android broker] Starting ActionView activity to " + appLink);
            activity.StartActivity(new Intent(Intent.ActionView, AndroidNative.Net.Uri.Parse(appLink)));

            throw new MsalClientException(
                MsalError.BrokerApplicationRequired,
                MsalErrorMessage.BrokerApplicationRequired);
        }
    }
}
