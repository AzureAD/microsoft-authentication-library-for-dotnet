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

namespace Microsoft.Identity.Client.Platforms.Android
{
    internal class AndroidBrokerProxy
    {
        private const string RedirectUriScheme = "msauth";
        private const string BrokerTag = BrokerConstants.Signature;
        public const string WorkAccount = "com.microsoft.workaccount.user.info";

        private readonly Context _androidContext;
        private readonly AccountManager _androidAccountManager;
        private readonly ICoreLogger _logger;

        public AndroidBrokerProxy(Context androidContext, ICoreLogger logger)
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
            // 2- permissions are set in the manifest,
            // 3- if package is not broker itself for both company portal and azure
            // authenticator
            // 4- signature of the broker is valid
            // 5- account exists
            return VerifyManifestPermissions()
                   && VerifyAuthenticator(_androidAccountManager)
                   && CheckAccount(_androidAccountManager, "", "")
                   && !packageName.Equals(BrokerConstants.PackageName, StringComparison.OrdinalIgnoreCase)
                   && !packageName
                       .Equals(BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase);
        }

        public bool VerifyUser(string username, string uniqueid)
        {
            return CheckAccount(_androidAccountManager, username, uniqueid);
        }

        public AdalResultWrapper GetAuthTokenInBackground(IDictionary<string, string> brokerPayload, Activity callerActivity)
        {
            AdalResultWrapper authResult = null;
            VerifyNotOnMainThread();

            // if there is not any user added to account, it returns empty
            Account targetAccount = null;

            _logger.Info("BrokerProxy: Getting the broker work and school accounts ");
            Account[] accountList = _androidAccountManager
                .GetAccountsByType(BrokerConstants.BrokerAccountType);

            if (accountList != null && accountList.Length > 0)
            {
                _logger.Info("BrokerProxy: The broker found some accounts");
            }


            if (!string.IsNullOrEmpty(request.BrokerAccountName))
            {
                targetAccount = FindAccount(request.BrokerAccountName, accountList);
                _logger.Verbose("BrokerProxy: Found account based on the broker account name? " + (targetAccount != null));
            }
            else
            {
                try
                {
                    _logger.Verbose("BrokerProxy: No broker account - getting broker users");
                    UserInfo[] users = GetBrokerUsers();

                    if (users != null && users.Length > 0)
                    {
                        _logger.Verbose("Broker Proxy: Found some broker users");
                    }

                    UserInfo matchingUser = FindUserInfo(request.UserId, users);
                    _logger.Info($"BrokerProxy: Found a matching user? " + (matchingUser != null));

                    if (matchingUser != null)
                    {
                        targetAccount = FindAccount(matchingUser.DisplayableId, accountList);
                        _logger.Info($"BrokerProxy: Found a matching account based on the user? " + (targetAccount != null));
                    }
                }
                catch (Exception e)
                {
                    _logger.ErrorPii(e);
                }
            }

            if (targetAccount != null)
            {
                Bundle brokerOptions = GetBrokerOptions(request);

                // blocking call to get token from cache or refresh request in
                // background at Authenticator
                IAccountManagerFuture result = null;
                try
                {
                    // It does not expect activity to be launched.
                    // AuthenticatorService is handling the request at
                    // AccountManager.

                    _logger.Info("BrokerProxy: Invoking the actual broker to get a token");

                    result = _androidAccountManager.GetAuthToken(
                        targetAccount,
                        BrokerConstants.AuthtokenType,
                        brokerOptions,
                        false,
                        null /* set to null to avoid callback */,
                        new Handler(callerActivity.MainLooper));

                    // Making blocking request here
                    _logger.Info("BrokerProxy: Received result from Authenticator? " + (result != null));

                    Bundle bundleResult = (Bundle)result.GetResult(10000, TimeUnit.Milliseconds);
                    // Authenticator should throw OperationCanceledException if
                    // token is not available
                    authResult = GetResultFromBrokerResponse(bundleResult);
                }
                catch (OperationCanceledException e)
                {
                    _logger.ErrorPii(e);
                }
                catch (AuthenticatorException e)
                {
                    _logger.ErrorPii(e);
                }
                catch (Java.Lang.Exception javaException)
                {
                    _logger.ErrorPii(javaException);
                }
                catch (Exception e)
                {
                    // Authenticator gets problem from webrequest or file read/write
                    /*                    Logger.e(TAG, "Authenticator cancels the request", "",
                                                ADALError.BROKER_AUTHENTICATOR_IO_EXCEPTION);*/

                    _logger.ErrorPii(e);
                }

                _logger.Info("BrokerProxy: Returning result from Authenticator ? " + (authResult != null));

                return authResult;
            }
            else
            {
                _logger.Warning("Target account is not found");
            }

            return null;
        }

        public Intent GetIntentForBrokerActivity(IDictionary<string, string> brokerPayload, Activity callerActivity)
        {
            Intent intent = null;

            try
            {
                IAccountManagerFuture result = null;
                // Callback is not passed since it is making a blocking call to get
                // intent. Activity needs to be launched from calling app
                // to get the calling app's metadata if needed at BrokerActivity.
                Bundle addAccountOptions = GetBrokerOptions(request);

                _logger.Info("BrokerProxy: Broker Account Name: " + request.BrokerAccountName);

                result = _androidAccountManager.AddAccount(BrokerConstants.BrokerAccountType,
                    BrokerConstants.AuthtokenType, null, addAccountOptions, null,
                    null, GetPreferredLooper(callerActivity));

                if (result == null)
                {
                    _logger.Info("BrokerProxy: Android account manager AddAccount didn't return any results. ");
                }

                // Making blocking request here
                Bundle bundleResult = (Bundle)result?.Result;
                // Authenticator should throw OperationCanceledException if
                // token is not available
                intent = (Intent)bundleResult?.GetParcelable(AccountManager.KeyIntent);

                // Add flag to this intent to signal that request is for broker logic
                if (intent != null)
                {
                    _logger.Info("BrokerProxy: Intent created from BundleResult is not null. ");
                    intent.PutExtra(BrokerConstants.BrokerRequest, BrokerConstants.BrokerRequest);
                }
                else
                {
                    _logger.Info("BrokerProxy: Intent created from BundleResult is null. ");
                    throw new AdalException(AdalErrorAndroidEx.NullIntentReturnedFromBroker, AdalErrorMessageAndroidEx.NullIntentReturnedFromBroker);
                }
            }
            catch (AdalException ex)
            {
                _logger.ErrorPii(ex);
                throw;
            }
            catch (Exception e)
            {
                _logger.ErrorPii(e);
            }

            return intent;
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

        // App needs to give permission to AccountManager to use broker.
        private bool VerifyManifestPermissions()
        {
            return VerifyManifestPermission("android.permission.GET_ACCOUNTS") &&
                   VerifyManifestPermission("android.permission.MANAGE_ACCOUNTS") &&
                   VerifyManifestPermission("android.permission.USE_CREDENTIALS");
        }

        private bool VerifyManifestPermission(string permission)
        {
            if (Permission.Granted !=
                Application.Context.PackageManager.CheckPermission(permission, Application.Context.PackageName))
            {
                _logger.Warning(string.Format(CultureInfo.InvariantCulture,
                    AdalErrorMessageAndroidEx.MissingPackagePermissionTemplate, permission));

                return false;
            }
            return true;
        }

        private void VerifyNotOnMainThread()
        {
            Looper looper = Looper.MyLooper();
            if (looper != null && looper == _androidContext.MainLooper)
            {
                Exception exception = new AdalException(
                    "Calling this from your main thread can lead to deadlock");
                _logger.ErrorPii(exception);

                if (_androidContext.ApplicationInfo.TargetSdkVersion >= BuildVersionCodes.Froyo)
                {
                    throw exception;
                }
            }
        }

        private Account FindAccount(string accountName, Account[] accountList)
        {
            _logger.VerbosePii("BrokerProxy: Finding Account: " + accountName, "- BrokerProxy: finding account...");

            if (accountList != null)
            {
                foreach (Account account in accountList)
                {
                    bool found = account != null &&
                                 !string.IsNullOrEmpty(account.Name) &&
                                 account.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase);

                    _logger.VerbosePii(
                        $"Broker Proxy: Looking for a match at broker account {account?.Name}. Found? {found}",
                        $"Found? {found}");

                    if (found)
                    {
                        return account;
                    }
                }
            }

            return null;
        }

        private UserInfo FindUserInfo(string userid, UserInfo[] userList)
        {
            if (userList != null)
            {
                foreach (UserInfo user in userList)
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

        private AdalResultWrapper GetResultFromBrokerResponse(Bundle bundleResult)
        {
            if (bundleResult == null)
            {
                throw new AdalException("bundleResult in broker response is null");
            }

            int errCode = bundleResult.GetInt(AccountManager.KeyErrorCode);
            string msg = bundleResult.GetString(AccountManager.KeyErrorMessage);
            if (!string.IsNullOrEmpty(msg))
            {
                throw new AdalException(errCode.ToString(CultureInfo.InvariantCulture), msg);
            }
            else
            {
                bool initialRequest = bundleResult.ContainsKey(BrokerConstants.AccountInitialRequest);
                if (initialRequest)
                {
                    // Initial request from app to Authenticator needs to launch
                    // prompt. null resultEx means initial request
                    _logger.Info("BrokerProxy: Initial request - not returning a token");
                    return null;
                }

                // IDtoken is not present in the current broker user model
                AdalUserInfo adalUserinfo = GetUserInfoFromBrokerResult(bundleResult);
                AdalResult result =
                    new AdalResult("Bearer", bundleResult.GetString(AccountManager.KeyAuthtoken),
                        ConvertFromTimeT(bundleResult.GetLong("account.expiredate", 0)))
                    {
                        UserInfo = adalUserinfo
                    };

                result.UpdateTenantAndUserInfo(bundleResult.GetString(BrokerConstants.AccountUserInfoTenantId), null,
                    adalUserinfo);

                return new AdalResultWrapper
                {
                    Result = result,
                    RefreshToken = null,
                    ResourceInResponse = null,
                };
            }
        }

        internal static DateTimeOffset ConvertFromTimeT(long seconds)
        {
            var startTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
            return startTime.AddMilliseconds(seconds);
        }

        private static AdalUserInfo GetUserInfoFromBrokerResult(Bundle bundle)
        {
            // Broker has one user and related to ADFS WPJ user. It does not return
            // idtoken
            string userid = bundle.GetString(BrokerConstants.AccountUserInfoUserId);
            string givenName = bundle
                .GetString(BrokerConstants.AccountUserInfoGivenName);
            string familyName = bundle
                .GetString(BrokerConstants.AccountUserInfoFamilyName);
            string identityProvider = bundle
                .GetString(BrokerConstants.AccountUserInfoIdentityProvider);
            string displayableId = bundle
                .GetString(BrokerConstants.AccountUserInfoUserIdDisplayable);
            return new AdalUserInfo
            {
                UniqueId = userid,
                GivenName = givenName,
                FamilyName = familyName,
                IdentityProvider = identityProvider,
                DisplayableId = displayableId
            };
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

        private Bundle GetBrokerOptions(AuthenticationRequest request)
        {
            Bundle brokerOptions = new Bundle();
            // request needs to be parcelable to send across process
            brokerOptions.PutInt("com.microsoft.aad.adal:RequestId", request.RequestId);
            brokerOptions.PutString(BrokerConstants.AccountAuthority,
                request.Authority);
            brokerOptions.PutInt("json", 1);
            brokerOptions.PutString(BrokerConstants.AccountResource,
                request.Resource);

            ValidateBrokerRedirectURI(request);

            brokerOptions.PutString(BrokerConstants.AccountRedirect, request.RedirectUri);
            brokerOptions.PutString(BrokerConstants.AccountClientIdKey,
                request.ClientId);
            brokerOptions.PutString(BrokerConstants.AdalVersionKey,
                request.Version);
            brokerOptions.PutString(BrokerConstants.AccountExtraQueryParam,
                request.ExtraQueryParamsAuthentication);

            brokerOptions.PutString(BrokerConstants.CallerInfoPackage, Application.Context.PackageName);
            brokerOptions.PutInt(BrokerConstants.CallerInfoUID, Android.OS.Process.MyPid());

            if (request.Claims != null)
            {
                brokerOptions.PutString(BrokerConstants.SkipCache, Boolean.TrueString.ToLowerInvariant());
                brokerOptions.PutString(BrokerConstants.Claims, request.Claims);
            }

            if (request.CorrelationId != null)
            {
                brokerOptions.PutString(BrokerConstants.AccountCorrelationId, request
                    .CorrelationId.ToString());
            }

            string username = request.BrokerAccountName;
            if (string.IsNullOrEmpty(username))
            {
                username = request.LoginHint;
            }

            brokerOptions.PutString(BrokerConstants.AccountLoginHint, username);
            brokerOptions.PutString(BrokerConstants.AccountName, username);

            return brokerOptions;
        }

        private void ValidateBrokerRedirectURI(AuthenticationRequest request)
        {
            //During the silent broker flow, the redirect URI will be null.
            if (string.IsNullOrEmpty(request.RedirectUri))
            {
                return;
            }

            string computedRedirectUri = GetRedirectUriForBroker();

            if (!string.Equals(computedRedirectUri, request.RedirectUri, StringComparison.OrdinalIgnoreCase))
            {
                string msg = string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.BrokerRedirectUriIncorrectFormat, computedRedirectUri);
                _logger.Info(msg);
                throw new AdalException(AdalError.BrokerRedirectUriIncorrectFormat, msg);
            }
        }

        private bool CheckAccount(AccountManager am, string username, string uniqueId)
        {
            AuthenticatorDescription[] authenticators = am.GetAuthenticatorTypes();
            _logger.Verbose("BrokerProxy: CheckAccount. Getting authenticator types " + (authenticators?.Length ?? 0));

            foreach (AuthenticatorDescription authenticator in authenticators)
            {
                if (authenticator.Type.Equals(BrokerConstants.BrokerAccountType, StringComparison.OrdinalIgnoreCase))
                {
                    Account[] accountList = _androidAccountManager
                        .GetAccountsByType(BrokerConstants.BrokerAccountType);

                    _logger.Verbose("BrokerProxy: Getting the account list " + (accountList?.Length ?? 0));

                    string packageName;

                    if (authenticator.PackageName
                        .Equals(BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        packageName = BrokerConstants.AzureAuthenticatorAppPackageName;
                    }
                    else if (authenticator.PackageName
                        .Equals(BrokerConstants.PackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        packageName = BrokerConstants.PackageName;
                    }
                    else
                    {
                        _logger.Warning("BrokerProxy: Could not find the broker package so checking the account failed");
                        return false;
                    }

                    _logger.Verbose("BrokerProxy: Package name is " + packageName);

                    // Existing broker logic only connects to broker for token
                    // requests if account exists. New version can allow to
                    // add accounts through Adal.
                    if (HasSupportToAddUserThroughBroker(packageName))
                    {
                        _logger.Verbose("BrokerProxy: Broker supports adding accounts");
                        return true;
                    }
                    else if (accountList != null && accountList.Length > 0)
                    {
                        _logger.Verbose("BrokerProxy: Broker does not support adding accounts but some accounts are configured. Verifying if an account can be used...");
                        return VerifyAccount(accountList, username, uniqueId);
                    }
                }
            }

            _logger.Warning("BrokerProxy: Could not verify that an account can be used");
            return false;
        }

        private bool VerifyAccount(Account[] accountList, string username, string uniqueId)
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
                UserInfo[] users;
                try
                {
                    users = GetBrokerUsers();
                    UserInfo matchingUser = FindUserInfo(uniqueId, users);
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

            // if username or uniqueid not specified, it should use the broker
            // account.
            _logger.Verbose("BrokerProxy: Account verification passed");
            return true;
        }

        private bool HasSupportToAddUserThroughBroker(string packageName)
        {
            Intent intent = new Intent();
            intent.SetPackage(packageName);
            intent.SetClassName(packageName, packageName + ".ui.AccountChooserActivity");

            PackageManager packageManager = _androidContext.PackageManager;
            IList<ResolveInfo> infos = packageManager.QueryIntentActivities(intent, 0);
            return infos.Count > 0;
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
                throw new AdalException(AdalErrorAndroidEx.SignatureVerificationFailed, "No matching signature found");
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
                            throw new AdalException(AdalErrorAndroidEx.SignatureVerificationFailed,
                                string.Format(CultureInfo.InvariantCulture,
                                    "app certificate validation failed with {0}", chainStatus.Status));
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
                throw new AdalException(AdalErrorAndroidEx.SignatureVerificationFailed,
                    "No broker package found.");
            }

            if (packageInfo.Signatures == null || packageInfo.Signatures.Count == 0)
            {
                throw new AdalException(AdalErrorAndroidEx.SignatureVerificationFailed,
                    "No signature associated with the broker package.");
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


        private UserInfo[] GetBrokerUsers()
        {
            // Calling this on main thread will cause exception since this is
            // waiting on AccountManagerFuture
            if (Looper.MyLooper() == Looper.MainLooper)
            {
                throw new AdalException("Calling getBrokerUsers on main thread");
            }

            Account[] accountList = _androidAccountManager
                .GetAccountsByType(BrokerConstants.BrokerAccountType);
            Bundle bundle = new Bundle();
            bundle.PutBoolean(WorkAccount, true);

            if (accountList != null)
            {
                // get info for each user
                UserInfo[] users = new UserInfo[accountList.Length];
                for (int i = 0; i < accountList.Length; i++)
                {
                    // Use AccountManager Api method to get extended user info
                    IAccountManagerFuture result = _androidAccountManager.UpdateCredentials(
                        accountList[i], BrokerConstants.AuthtokenType, bundle,
                        null, null, null);

                    _logger.Verbose("Waiting for the result");

                    Bundle userInfoBundle = (Bundle)result.Result;

                    users[i] = new UserInfo
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
    }
}
