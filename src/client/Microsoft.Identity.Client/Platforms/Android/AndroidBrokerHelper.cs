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

            _logger.Verbose("BrokerProxy: Getting the Android context");
            _androidAccountManager = AccountManager.Get(_androidContext);
        }

        public bool CanSwitchToBroker()
        {
            string packageName = _androidContext.PackageName;

            // ADAL switches broker for following conditions:
            // 1- app is not skipping the broker
            // 2- if package is not broker itself for both company portal and azure
            // authenticator
            // 3- signature of the broker is valid
            // 4- account exists

            //Force this to return true for broker test app
            return VerifyAuthenticator(_androidAccountManager)
                   && CheckForBrokerAccount(_androidAccountManager, "", "")
                   && !packageName.Equals(BrokerConstants.PackageName, StringComparison.OrdinalIgnoreCase)
                   && !packageName
           .Equals(BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase);
        }

        public bool VerifyUser(string username, string uniqueid)
        {
            return CheckForBrokerAccount(_androidAccountManager, username, uniqueid);
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

                _logger.Info("BrokerProxy: Broker Account Name: " + GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username));

                result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                    BrokerConstants.AuthtokenType, null, addAccountOptions, null,
                    null, GetPreferredLooper(callerActivity));

                if (result == null)
                {
                    _logger.Info("BrokerProxy: Android account manager AddAccount didn't return any results. ");
                }

                Bundle bundleResult = (Bundle)result?.Result;
                // Broker should throw OperationCanceledException if token is not available
                intent = (Intent)bundleResult?.GetParcelable(AccountManager.KeyIntent);

                //Validate that the intent was created succsesfully.
                if (intent != null)
                {
                    _logger.Info("BrokerProxy: Intent created from BundleResult is not null. ");
                    // Need caller info UID for broker communication
                    intent.PutExtra(BrokerConstants.CallerInfoUID, Binder.CallingUid);
                }
                else
                {
                    _logger.Info("BrokerProxy: Intent created from BundleResult is null. ");
                    throw new MsalException(MsalError.NullIntentReturnedFromBroker, MsalErrorMessage.NullIntentReturnedFromBroker);
                }

                intent = GetInteractiveBrokerIntent(brokerPayload, intent);
            }
            catch (Exception e)
            {
                _logger.Error("Error when trying to acquire intent for broker authentication.");
                throw e;
            }

            return intent;
        }

        public Bundle GetAuthTokenSilently(IDictionary<string, string> brokerPayload, Activity callerActivity)
        {
            Bundle silentOperationBundle = GetSilentBrokerIntent(brokerPayload);
            silentOperationBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, BrokerConstants.AcquireTokenSilent);

            _logger.Info("BrokerProxy: Broker Account Name: " + GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Username));

            var result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                BrokerConstants.AuthtokenType, null, silentOperationBundle, null,
                null, GetPreferredLooper(callerActivity));

            if (result == null)
            {
                _logger.Info("BrokerProxy: Android account manager AddAccount didn't return any results. ");
            }

            Bundle bundleResult = (Bundle)result?.Result;
            if (bundleResult.GetBoolean(BrokerConstants.BrokerRequestV2Success))
            {
                return bundleResult;
            }

            return null;
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
                    _logger.Info("BrokerProxy: Android account manager AddAccount didn't return any results. ");
                }

                Bundle bundleResult = (Bundle)result?.Result;

                var bpKey = bundleResult.GetString(BrokerConstants.NegotiatedBPVersionKey);
                var test = bundleResult.GetString("common.protocol.version");
                if (!String.IsNullOrEmpty(bpKey))
                {
                    //Log Message Here with version
                    return true;
                }
                else
                {
#pragma warning disable CA2201 // Do not raise reserved exception types
                    throw new Exception();
#pragma warning restore CA2201 // Do not raise reserved exception types
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error when trying to acquire intent for broker authentication.");
                throw e;
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
                _logger.Info("callerActivity.MainLooper returned: " + callerActivity.MainLooper.ToString());
                return new Handler(callerActivity.MainLooper);
            }
        }

        private IAccount FindUserInfo(string userid, IAccount[] userList)
        {
            if (userList != null)
            {
                foreach (Account user in userList)
                {
                    if (user != null && !string.IsNullOrEmpty(user.UniqueId)
                        && user.UniqueId.Equals(userid, StringComparison.OrdinalIgnoreCase))
                    {
                        return user;
                    }
                }
            }

            return null;
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
                Scopes = "user.read",
                RedirectUri = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.RedirectUri),
                ClientId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.ClientId),
                ClientAppName = Application.Context.PackageName,//"com.microsoft.identity.client",
                ClientAppVersion = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MatchAll).VersionName,
                ClientVersion = "4.4.0",
                CorrelationId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.CorrelationId),
                Prompt = "NONE"
            };

            var test = JsonHelper.SerializeToJson(request);
            brokerIntent.PutExtra(BrokerConstants.BrokerRequestV2, test);

            return brokerIntent;
        }

        private Bundle GetSilentBrokerIntent(IDictionary<string, string> brokerPayload)
        {
            ValidateBrokerRedirectURI(brokerPayload);
            Bundle bundle = new Bundle();

            BrokerRequest request = new BrokerRequest
            {
                Authority = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Authority),
                Scopes = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.Scope),
                RedirectUri = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.RedirectUri),
                ClientId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.ClientId),
                ClientAppName = Application.Context.PackageName,//"com.microsoft.identity.client",
                ClientAppVersion = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MatchAll).VersionName,
                ClientVersion = "4.4.0",
                CorrelationId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.CorrelationId),
                Prompt = "NONE",
                HomeAccountId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.HomeAccountId),
                LocalAccountId = GetValueFromBrokerPayload(brokerPayload, BrokerParameter.LocalAccountId)
            };

            var test = JsonHelper.SerializeToJson(request);
            bundle.PutString(BrokerConstants.BrokerRequestV2, test);
            bundle.PutInt(BrokerConstants.CallerInfoUID, Binder.CallingUid);

            return bundle;
        }

        private bool CheckForBrokerAccount(AccountManager am, string username, string uniqueId)
        {
            AuthenticatorDescription[] authenticators = am.GetAuthenticatorTypes();
            _logger.Verbose("BrokerProxy: CheckAccount. Getting authenticator types " + (authenticators?.Length ?? 0));

            foreach (AuthenticatorDescription authenticator in authenticators)
            {
                if (authenticator.Type.Equals(BrokerConstants.BrokerAccountType, StringComparison.OrdinalIgnoreCase))
                {
                    AndroidNative.Accounts.Account[] accountList = _androidAccountManager
                        .GetAccountsByType(BrokerConstants.BrokerAccountType);

                    _logger.Verbose("BrokerProxy: Getting the account list " + (accountList?.Length ?? 0));

                    string packageName;

                    if (authenticator.PackageName
                        .Equals(BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        packageName = BrokerConstants.AzureAuthenticatorAppPackageName;
                    }
                    else if (authenticator.PackageName
                        .Equals("BROKER_ACCOUNT_MANAGER_OPERATION_KEY", StringComparison.OrdinalIgnoreCase))
                    {
                        packageName = BrokerConstants.PackageName;
                    }
                    else
                    {
                        _logger.Warning("BrokerProxy: Could not find the broker package so checking the account failed");
                        return false;
                    }

                    _logger.Verbose("BrokerProxy: Package name is " + packageName);

                    return VerifyBrokerAccountExists(accountList, username, uniqueId);
                }
            }

            _logger.Warning("BrokerProxy: Could not verify that an account can be used");
            return false;
        }

        private bool VerifyBrokerAccountExists(AndroidNative.Accounts.Account[] accountList, string username, string uniqueId)
        {
            _logger.Verbose("BrokerProxy: starting account verification");

            if (!string.IsNullOrEmpty(username))
            {
                bool found = username.Equals(accountList[0].Name, StringComparison.OrdinalIgnoreCase);
                _logger.Verbose("BrokerProxy: Found an account that matches the username? " + false);
                return found;
            }

            if (!string.IsNullOrEmpty(uniqueId))
            {
                // Uniqueid for account at authenticator is not available with
                // Account
                IAccount[] users;
                try
                {
                    users = GetBrokerUsers();
                    IAccount matchingUser = FindUserInfo(uniqueId, users);
                    return matchingUser != null;
                }
                catch (Exception e)
                {
                    _logger.Error("BrokerProxy: Could not verify an account because of an exception.");
                    _logger.ErrorPii(e);
                }

                _logger.Warning("BrokerProxy: Could not verify the account");

                return false;
            }

            _logger.Verbose("BrokerProxy: Account verification passed");
            return true;
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
                    _logger.Verbose("BrokerProxy: Found the Authenticator on the device");
                    return true;
                }
            }

            _logger.Warning("BrokerProxy: No Authenticator found on the device.");
            return false;
        }


        private IAccount[] GetBrokerUsers()
        {
            // Calling this on main thread will cause exception since this is
            // waiting on AccountManagerFuture
            if (Looper.MyLooper() == Looper.MainLooper)
            {
                throw new MsalClientException("Calling getBrokerUsers on main thread");
            }

            AndroidNative.Accounts.Account[] accountList = _androidAccountManager
                .GetAccountsByType(BrokerConstants.BrokerAccountType);
            Bundle bundle = new Bundle();
            bundle.PutBoolean(WorkAccount, true);

            if (accountList != null)
            {
                // get info for each user
                Account[] users = new Account[accountList.Length];
                for (int i = 0; i < accountList.Length; i++)
                {
                    // Use AccountManager Api method to get extended user info
                    IAccountManagerFuture result = _androidAccountManager.UpdateCredentials(
                        accountList[i], BrokerConstants.AuthtokenType, bundle,
                        null, null, null);

                    _logger.Verbose("Waiting for the result");

                    Bundle userInfoBundle = (Bundle)result.Result;

                    users[i] = new Account(null, null, null)
                    {
                        UniqueId = userInfoBundle
                            .GetString(BrokerConstants.AccountUserInfoUserId),
                        GivenName = userInfoBundle
                            .GetString(BrokerConstants.AccountUserInfoGivenName),
                        FamilyName = userInfoBundle
                            .GetString(BrokerConstants.AccountUserInfoFamilyName),
                        IdentityProvider = userInfoBundle
                            .GetString(BrokerConstants.AccountUserInfoIdentityProvider),
                        DisplayableId = userInfoBundle
                            .GetString(BrokerConstants.AccountUserInfoUserIdDisplayable),
                    };
                }

                return users;
            }
            return null;
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
