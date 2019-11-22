// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using AndroidNative = Android;
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Content.PM;
using Android.Util;
using Java.Security;
using Java.Util.Concurrent;
using OperationCanceledException = Android.Accounts.OperationCanceledException;
using Permission = Android.Content.PM.Permission;
using Signature = Android.Content.PM.Signature;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Platforms.Android
{
    internal class AndroidBrokerHelper
    {
        private const string RedirectUriScheme = "msauth";
        private const string BrokerTag = BrokerConstants.Signature;
        public const string WorkAccount = "com.microsoft.workaccount.user.info";

        private readonly Context _androidContext;
        private readonly AccountManager _androidAccountManager;
        private readonly ICoreLogger _logger;

        public AndroidBrokerHelper(Context androidContext, ICoreLogger logger)
        {
            _androidContext = androidContext ?? throw new ArgumentNullException(nameof(androidContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.Verbose("Getting the Android context");
            _androidAccountManager = AccountManager.Get(_androidContext);
        }

        public bool CanSwitchToBroker()
        {
            string packageName = _androidContext.PackageName;

            // ADAL switches broker for following conditions:
            // 1- app is not skipping the broker
            // 2- signature of the broker is valid
            // 3- account exists

            //Force this to return true for broker test app

            return VerifyAuthenticator(_androidAccountManager)
                   && !packageName.Equals(BrokerConstants.PackageName, StringComparison.OrdinalIgnoreCase)
                   && !packageName
           .Equals(BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase);
        }

        public Intent GetIntentForInteractiveBrokerRequest(IDictionary<string, string> brokerPayload, Activity callerActivity)
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

                _logger.Info("Broker Account Name: " + GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username));

                result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                    BrokerConstants.AuthtokenType, null, addAccountOptions, null,
                    null, GetPreferredLooper(callerActivity));

                if (result == null)
                {
                    _logger.Info("Android account manager AddAccount didn't return any results. ");
                }

                Bundle bundleResult = (Bundle)result?.Result;
                // Broker should throw OperationCanceledException if token is not available
                intent = (Intent)bundleResult?.GetParcelable(AccountManager.KeyIntent);

                //Validate that the intent was created succsesfully.
                if (intent != null)
                {
                    _logger.Info("Intent created from BundleResult is not null. ");
                    // Need caller info UID for broker communication
                    intent.PutExtra(BrokerConstants.CallerInfoUID, Binder.CallingUid);
                }
                else
                {
                    _logger.Info("Intent created from BundleResult is null. ");
                    throw new MsalException(MsalError.NullIntentReturnedFromBroker, MsalErrorMessage.NullIntentReturnedFromBroker);
                }

                intent = GetInteractiveBrokerIntent(brokerPayload, intent);
            }
            catch
            {
                _logger.Error("Error when trying to acquire intent for broker authentication.");
                throw;
            }

            return intent;
        }

        public string GetAuthTokenSilently(IDictionary<string, string> brokerPayload, Activity callerActivity)
        {
            GetBrokerAccountInfo(brokerPayload, callerActivity);
            Bundle silentOperationBundle = GetSilentBrokerBundle(brokerPayload);
            silentOperationBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.AcquireTokenSilent);

            _logger.Info("Broker Account Name: " + GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username));

            var result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                BrokerConstants.AuthtokenType, null, silentOperationBundle, null,
                null, GetPreferredLooper(callerActivity));

            if (result == null)
            {
                _logger.Info("Android Broker AddAccount didn't return any results. ");
            }

            Bundle bundleResult = (Bundle)result?.Result;
            if (bundleResult.GetBoolean(BrokerConstants.BrokerRequestV2Success))
            {
                _logger.Info("Android Broker succsesfully refreshd the access token.");
                return bundleResult.GetString(BrokerConstants.BrokerResultV2);
            }

            return null;
        }

        public void GetBrokerAccountInfo(IDictionary<string, string> brokerPayload, Activity callerActivity)
        {
            var loginHint = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.LoginHint);
            Bundle getAccountsBundle = GetBrokerAccountBundle(brokerPayload);
            getAccountsBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.GetAccounts);

            _logger.Info("Broker Account Name: " + loginHint);

            var result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                BrokerConstants.AuthtokenType, null, getAccountsBundle, null,
                null, GetPreferredLooper(callerActivity));

            if (result == null)
            {
                _logger.Info("Android account manager AddAccount didn't return any accounts. ");
                throw new MsalException(MsalError.NoBrokerAccountFound, "Please add the selected account to the broker");
            }

            Bundle bundleResult = (Bundle)result?.Result;
            var accounts = bundleResult.GetString(BrokerConstants.BrokerAccounts);

            if (!string.IsNullOrEmpty(accounts))
            {
                dynamic authResult = JArray.Parse(accounts);

                foreach (JObject account in authResult)
                {
                    var accountData = account[BrokerResponseConst.Account];
                    if ((accountData[BrokerResponseConst.UserName]).ToString() == loginHint)
                    {
                        brokerPayload.Add(BrokerParameter.HomeAccountId, accountData[BrokerResponseConst.HomeAccountId].ToString());
                        brokerPayload.Add(BrokerParameter.LocalAccountId, accountData[BrokerResponseConst.LocalAccountId].ToString());
                        var acc = account;
                        return;
                    }
                }
            }
        }

        public bool SayHelloToBroker(Activity callerActivity)
        {
            try
            {
                IAccountManagerFuture result = null;
                // Callback is not passed since it is making a blocking call to get
                // intent. Activity needs to be launched from calling app
                // to get the calling app's metadata if needed at BrokerActivity.

                Bundle HelloBundle = new Bundle();
                HelloBundle.PutString(BrokerConstants.ClientAdvertisedMaximumBPVersionKey, BrokerConstants.BrokerProtocalVersionCode);
                HelloBundle.PutString(BrokerConstants.ClientConfiguredMinimumBPVersionKey, "2.0");
                HelloBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, "HELLO");

                result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                    BrokerConstants.AuthtokenType, null, HelloBundle, null,
                    null, GetPreferredLooper(callerActivity));

                if (result == null)
                {
                    _logger.Info("Android account manager AddAccount didn't return any results. ");
                }

                Bundle bundleResult = (Bundle)result?.Result;

                var bpKey = bundleResult.GetString(BrokerConstants.NegotiatedBPVersionKey);
                var test = bundleResult.GetString(BrokerConstants.CommonProtocolVersion);
                if (!String.IsNullOrEmpty(bpKey))
                {
                    _logger.Info("Using broker protocol version: " + bpKey);
                    return true;
                }
                else
                {
                    throw new MsalException("Could not negotiate protocol version with broker");
                }
            }
            catch
            {
                _logger.Error("Error when trying to acquire intent for broker authentication.");
                throw;
            }
        }

        private Handler GetPreferredLooper(Activity callerActivity)
        {
            var myLooper = Looper.MyLooper();
            if (myLooper != null && callerActivity.MainLooper != myLooper)
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

        private void ValidateBrokerRedirectURI(IDictionary<string, string> brokerPayload)
        {
            //During the silent broker flow, the redirect URI will be null.
            if (string.IsNullOrEmpty(GetValueFromBrokerPayload(brokerPayload, BrokerParameter.RedirectUri)))
            {
                return;
            }

            string computedRedirectUri = GetRedirectUriForBroker();

            if (!string.Equals(computedRedirectUri, GetValueFromBrokerPayload(brokerPayload, BrokerParameter.RedirectUri), StringComparison.OrdinalIgnoreCase))
            {
                //ADD Broker Error for redirect URI on andorid
                string msg = string.Format(CultureInfo.CurrentCulture, MsalError.CannotInvokeBroker, computedRedirectUri);
                _logger.Info(msg);
                throw new MsalClientException(MsalError.CannotInvokeBroker, msg);
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
                if (info != null && info.Signatures != null && info.Signatures.Count > 0)
                {
                    Signature signature = info.Signatures[0];
                    MessageDigest md = MessageDigest.GetInstance("SHA");
                    md.Update(signature.ToByteArray());
                    return Convert.ToBase64String(md.Digest(), Base64FormattingOptions.None);
                    // Server side needs to register all other tags. ADAL will
                    // send one of them.
                }
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

        private Intent GetInteractiveBrokerIntent(IDictionary<string, string> brokerPayload, Intent brokerIntent)
        {
            ValidateBrokerRedirectURI(brokerPayload);
            BrokerRequest request = new BrokerRequest
            {
                Authority = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Authority),
                Scopes = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Scope),
                RedirectUri = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.RedirectUri),
                ClientId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.ClientId),
                ClientAppName = Application.Context.PackageName,
                ClientAppVersion = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MatchAll).VersionName,
                ClientVersion = "4.4.0",
                CorrelationId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.CorrelationId),
                Prompt = "NONE"
            };

            brokerIntent.PutExtra(BrokerConstants.BrokerRequestV2, JsonHelper.SerializeToJson(request));

            return brokerIntent;
        }

        private Bundle GetSilentBrokerBundle(IDictionary<string, string> brokerPayload)
        {
            Bundle bundle = new Bundle();

            BrokerRequest request = new BrokerRequest
            {
                Authority = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Authority),
                Scopes = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Scope),
                RedirectUri = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.RedirectUri),
                ClientId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.ClientId),
                ClientAppName = Application.Context.PackageName,
                ClientAppVersion = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MatchAll).VersionName,
                ClientVersion = "4.4.0",
                CorrelationId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.CorrelationId),
                Prompt = "NONE",
                HomeAccountId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.HomeAccountId),
                LocalAccountId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.LocalAccountId)
            };

            bundle.PutString(BrokerConstants.BrokerRequestV2, JsonHelper.SerializeToJson(request));
            bundle.PutInt(BrokerConstants.CallerInfoUID, Binder.CallingUid);

            return bundle;
        }

        private Bundle GetBrokerAccountBundle(IDictionary<string, string> brokerPayload)
        {
            Bundle bundle = new Bundle();

            bundle.PutString(BrokerConstants.AccountClientIdKey, GetValueFromBrokerPayload(brokerPayload, BrokerParameter.ClientId));
            bundle.PutString(BrokerConstants.AccountRedirect, GetValueFromBrokerPayload(brokerPayload, BrokerParameter.ClientId));
            bundle.PutInt(BrokerConstants.CallerInfoUID, Binder.CallingUid);

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
                throw new MsalException(MsalError.BrokerSignatureVerificationFailed, "No matching signature found");
            }
        }

        private void VerifyCertificateChain(List<X509Certificate2> certificates)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection(certificates.ToArray());
            X509Chain chain = new X509Chain();
            chain.ChainPolicy = new X509ChainPolicy()
            {
                RevocationMode = X509RevocationMode.NoCheck
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
                            throw new MsalException(MsalError.BrokerSignatureVerificationFailed, 
                                string.Format(CultureInfo.InvariantCulture,"app certificate validation failed with {0}", chainStatus.Status));
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
                throw new MsalException(MsalError.BrokerSignatureVerificationFailed, "No broker package found");
            }

            if (packageInfo.Signatures == null || packageInfo.Signatures.Count == 0)
            {
                throw new MsalException(MsalError.BrokerSignatureVerificationFailed, "No signature associated with the broker package.");
            }

            List<X509Certificate2> certificates = new List<X509Certificate2>(packageInfo.Signatures.Count);
            foreach (Signature signature in packageInfo.Signatures)
            {
                byte[] rawCert = signature.ToByteArray();
                X509Certificate2 x509Certificate = null;
                x509Certificate = new X509Certificate2(rawCert);
                certificates.Add(x509Certificate);
            }

            return certificates;
        }

        private bool VerifyAuthenticator(AccountManager am)
        {
            // there may be multiple authenticators from same package
            // , but there is only one entry for an authenticator type in
            // AccountManager.
            // If another app tries to install same authenticator type, it will
            // queue up and will be active after first one is uninstalled.
            AuthenticatorDescription[] authenticators = am.GetAuthenticatorTypes();
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
