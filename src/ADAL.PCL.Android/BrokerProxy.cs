//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using Android.Util;
using Java.Security;
using Java.Util.Concurrent;
using OperationCanceledException = Android.Accounts.OperationCanceledException;
using Permission = Android.Content.PM.Permission;
using Signature = Android.Content.PM.Signature;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class BrokerProxy
    {
        private const string RedirectUriScheme = "msauth";
        private Context mContext;
        private AccountManager mAcctManager;
        private string mBrokerTag;
        public const string DATA_USER_INFO = "com.microsoft.workaccount.user.info";


        public BrokerProxy()
        {
            mBrokerTag = BrokerConstants.Signature;
        }

        public BrokerProxy(Context ctx)
        {
            mContext = ctx;
            mAcctManager = AccountManager.Get(mContext);
            mBrokerTag = BrokerConstants.Signature;
        }

        public bool CanSwitchToBroker()
        {
            string packageName = mContext.PackageName;

            // ADAL switches broker for following conditions:
            // 1- app is not skipping the broker
            // 2- permissions are set in the manifest,
            // 3- if package is not broker itself for both company portal and azure
            // authenticator
            // 4- signature of the broker is valid
            // 5- account exists
            return VerifyManifestPermissions()
                   && VerifyAuthenticator(mAcctManager)
                   && CheckAccount(mAcctManager, "", "")
                   && !packageName.Equals(BrokerConstants.PackageName, StringComparison.OrdinalIgnoreCase)
                   && !packageName
                       .Equals(BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase);
        }

        public bool VerifyUser(string username, string uniqueid)
        {
            return CheckAccount(mAcctManager, username, uniqueid);
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
                PlatformPlugin.Logger.Information(null,
                    string.Format(AdalErrorMessageAndroidEx.MissingPackagePermissionTemplate, permission));
                return false;
            }

            return true;
        }

        private void VerifyNotOnMainThread()
        {
            Looper looper = Looper.MyLooper();
            if (looper != null && looper == mContext.MainLooper)
            {
                Exception exception = new Exception(
                    "calling this from your main thread can lead to deadlock");
                PlatformPlugin.Logger.Error(null, exception);
                if (mContext.ApplicationInfo.TargetSdkVersion >= BuildVersionCodes.Froyo)
                {
                    throw exception;
                }
            }
        }

        private Account FindAccount(string accountName, Account[] accountList)
        {
            if (accountList != null)
            {
                foreach (Account account in accountList)
                {
                    if (account != null && account.Name != null
                        && account.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase))
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

        public AuthenticationResultEx GetAuthTokenInBackground(AuthenticationRequest request, Activity callerActivity)
        {
            AuthenticationResultEx authResult = null;
            VerifyNotOnMainThread();

            // if there is not any user added to account, it returns empty
            Account targetAccount = null;
            Account[] accountList = mAcctManager
                .GetAccountsByType(BrokerConstants.BrokerAccountType);

            if (!string.IsNullOrEmpty(request.BrokerAccountName))
            {
                targetAccount = FindAccount(request.BrokerAccountName, accountList);
            }
            else
            {
                try
                {
                    UserInfo[] users = GetBrokerUsers();
                    UserInfo matchingUser = FindUserInfo(request.UserId, users);
                    if (matchingUser != null)
                    {
                        targetAccount = FindAccount(matchingUser.DisplayableId, accountList);
                    }
                }
                catch (Exception e)
                {
                    PlatformPlugin.Logger.Error(null, e);
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
                    //
                    result = mAcctManager.GetAuthToken(targetAccount,
                        BrokerConstants.AuthtokenType, brokerOptions, false,
                        null /*
                              * set to null to avoid callback
                              */, new Handler(callerActivity.MainLooper));

                    // Making blocking request here
                    PlatformPlugin.Logger.Verbose(null, "Received result from Authenticator");
                    Bundle bundleResult = (Bundle) result.GetResult(10000, TimeUnit.Milliseconds);
                    // Authenticator should throw OperationCanceledException if
                    // token is not available
                    authResult = GetResultFromBrokerResponse(bundleResult);
                }
                catch (OperationCanceledException e)
                {
                    PlatformPlugin.Logger.Error(null, e);
                }
                catch (AuthenticatorException e)
                {
                    PlatformPlugin.Logger.Error(null, e);
                }
                catch (Exception e)
                {
                    // Authenticator gets problem from webrequest or file read/write
                    /*                    Logger.e(TAG, "Authenticator cancels the request", "",
                                                ADALError.BROKER_AUTHENTICATOR_IO_EXCEPTION);*/

                    PlatformPlugin.Logger.Error(null, e);
                }

                PlatformPlugin.Logger.Verbose(null, "Returning result from Authenticator");
                return authResult;
            }
            else
            {
                PlatformPlugin.Logger.Verbose(null, "Target account is not found");
            }

            return null;
        }

        private AuthenticationResultEx GetResultFromBrokerResponse(Bundle bundleResult)
        {
            if (bundleResult == null)
            {
                throw new Exception("bundleResult");
            }

            int errCode = bundleResult.GetInt(AccountManager.KeyErrorCode);
            string msg = bundleResult.GetString(AccountManager.KeyErrorMessage);
            if (!string.IsNullOrEmpty(msg))
            {
                throw new AdalException(errCode.ToString(), msg);
            }
            else
            {
                bool initialRequest = bundleResult.ContainsKey(BrokerConstants.AccountInitialRequest);
                if (initialRequest)
                {
                    // Initial request from app to Authenticator needs to launch
                    // prompt. null resultEx means initial request
                    return null;
                }

                // IDtoken is not present in the current broker user model
                UserInfo userinfo = GetUserInfoFromBrokerResult(bundleResult);
                AuthenticationResult result =
                    new AuthenticationResult("Bearer", bundleResult.GetString(AccountManager.KeyAuthtoken),
                        ConvertFromTimeT(bundleResult.GetLong("account.expiredate", 0)))
                    {
                        UserInfo = userinfo
                    };

                result.UpdateTenantAndUserInfo(bundleResult.GetString(BrokerConstants.AccountUserInfoTenantId), null,
                    userinfo);

                return new AuthenticationResultEx
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


        internal static UserInfo GetUserInfoFromBrokerResult(Bundle bundle)
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
            return new UserInfo
            {
                UniqueId = userid,
                GivenName = givenName,
                FamilyName = familyName,
                IdentityProvider = identityProvider,
                DisplayableId = displayableId
            };
        }

        public Intent GetIntentForBrokerActivity(AuthenticationRequest request, Activity callerActivity)
        {
            Intent intent = null;
            IAccountManagerFuture result = null;
            try
            {
                // Callback is not passed since it is making a blocking call to get
                // intent. Activity needs to be launched from calling app
                // to get the calling app's metadata if needed at BrokerActivity.
                Bundle addAccountOptions = GetBrokerOptions(request);
                result = mAcctManager.AddAccount(BrokerConstants.BrokerAccountType,
                    BrokerConstants.AuthtokenType, null, addAccountOptions, null,
                    null, new Handler(callerActivity.MainLooper));

                // Making blocking request here
                Bundle bundleResult = (Bundle) result.Result;
                // Authenticator should throw OperationCanceledException if
                // token is not available
                intent = (Intent) bundleResult.GetParcelable(AccountManager.KeyIntent);

                // Add flag to this intent to signal that request is for broker
                // logic
                if (intent != null)
                {
                    intent.PutExtra(BrokerConstants.BrokerRequest, BrokerConstants.BrokerRequest);
                }
            }
            catch (OperationCanceledException e)
            {
                PlatformPlugin.Logger.Error(null, e);
            }
            catch (Exception e)
            {
                // Authenticator gets problem from webrequest or file read/write
                PlatformPlugin.Logger.Error(null, new AdalException("Authenticator cancels the request", e));
            }

            return intent;
        }

        private string GetRedirectUriForBroker()
        {
            string packageName = Application.Context.PackageName;

            // First available signature. Applications can be signed with multiple
            // signatures.
            string signatureDigest = this.GetCurrentSignatureForPackage(packageName);
            if (!string.IsNullOrEmpty(signatureDigest))
            {
                return string.Format(CultureInfo.CurrentCulture, "{0}://{1}/{2}", RedirectUriScheme,
                    packageName.ToLower(), signatureDigest);
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
                PlatformPlugin.Logger.Information(null, "Calling App's package does not exist in PackageManager");
            }
            catch (NoSuchAlgorithmException)
            {
                PlatformPlugin.Logger.Information(null, "Digest SHA algorithm does not exists");
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
            string computedRedirectUri = GetRedirectUriForBroker();

            if (!string.IsNullOrEmpty(request.RedirectUri) && !string.Equals(computedRedirectUri, request.RedirectUri))
            {
                throw new ArgumentException("redirect uri for broker invocation should be set to" + computedRedirectUri);
            }

            brokerOptions.PutString(BrokerConstants.AccountRedirect, request.RedirectUri);
            brokerOptions.PutString(BrokerConstants.AccountClientIdKey,
                request.ClientId);
            brokerOptions.PutString(BrokerConstants.AdalVersionKey,
                request.Version);
            brokerOptions.PutString(BrokerConstants.AccountExtraQueryParam,
                request.ExtraQueryParamsAuthentication);
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

        private bool CheckAccount(AccountManager am, string username, string uniqueId)
        {
            AuthenticatorDescription[] authenticators = am.GetAuthenticatorTypes();
            foreach (AuthenticatorDescription authenticator in authenticators)
            {
                if (authenticator.Type.Equals(BrokerConstants.BrokerAccountType))
                {
                    Account[] accountList = mAcctManager
                        .GetAccountsByType(BrokerConstants.BrokerAccountType);

                    // Authenticator installed from Company portal
                    // This supports only one account
                    if (authenticator.PackageName
                        .Equals(BrokerConstants.PackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Adal should not connect if given username does not match
                        if (accountList != null && accountList.Length > 0)
                        {
                            return VerifyAccount(accountList, username, uniqueId);
                        }

                        return false;

                        // Check azure authenticator and allow calls for test
                        // versions
                    }
                    else if (authenticator.PackageName
                        .Equals(BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase)
                             || authenticator.PackageName
                                 .Equals(BrokerConstants.PackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Existing broker logic only connects to broker for token
                        // requests if account exists. New version can allow to
                        // add accounts through Adal.
                        if (HasSupportToAddUserThroughBroker())
                        {
                            PlatformPlugin.Logger.Verbose(null, "Broker supports to add user through app");
                            return true;
                        }
                        else if (accountList != null && accountList.Length > 0)
                        {
                            return VerifyAccount(accountList, username, uniqueId);
                        }
                    }
                }
            }

            return false;
        }

        private bool VerifyAccount(Account[] accountList, string username, string uniqueId)
        {
            if (!string.IsNullOrEmpty(username))
            {
                return username.Equals(accountList[0].Name, StringComparison.OrdinalIgnoreCase);
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
                    PlatformPlugin.Logger.Error(null, e);
                }

                PlatformPlugin.Logger.Verbose(null,
                    "It could not check the uniqueid from broker. It is not using broker");
                return false;
            }

            // if username or uniqueid not specified, it should use the broker
            // account.
            return true;
        }

        private bool HasSupportToAddUserThroughBroker()
        {
            Intent intent = new Intent();
            intent.SetPackage(BrokerConstants.AzureAuthenticatorAppPackageName);
            intent.SetClassName(BrokerConstants.AzureAuthenticatorAppPackageName,
                BrokerConstants.AzureAuthenticatorAppPackageName
                + ".ui.AccountChooserActivity");
            PackageManager packageManager = mContext.PackageManager;
            IList<ResolveInfo> infos = packageManager.QueryIntentActivities(intent, 0);
            return infos.Count > 0;
        }

        private bool VerifySignature(string brokerPackageName)
        {
            List< X509Certificate2> certs = ReadCertDataForBrokerApp(brokerPackageName);

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
                if (mBrokerTag.Equals(signatureHash) ||
                    BrokerConstants.AzureAuthenticatorAppSignature.Equals(signatureHash))
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
            PackageInfo packageInfo = mContext.PackageManager.GetPackageInfo(brokerPackageName,
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
                if (authenticator.Type.Equals(BrokerConstants.BrokerAccountType)
                    && VerifySignature(authenticator.PackageName))
                {
                    return true;
                }
            }

            return false;
        }


        public UserInfo[] GetBrokerUsers()
        {
            // Calling this on main thread will cause exception since this is
            // waiting on AccountManagerFuture
            if (Looper.MyLooper() == Looper.MainLooper)
            {
                throw new Exception("Calling getBrokerUsers on main thread");
            }

            Account[] accountList = mAcctManager
                .GetAccountsByType(BrokerConstants.BrokerAccountType);
            Bundle bundle = new Bundle();
            bundle.PutBoolean(DATA_USER_INFO, true);

            if (accountList != null)
            {
                // get info for each user
                UserInfo[] users = new UserInfo[accountList.Length];
                for (int i = 0; i < accountList.Length; i++)
                {
                    // Use AccountManager Api method to get extended user info
                    IAccountManagerFuture result = mAcctManager.UpdateCredentials(
                        accountList[i], BrokerConstants.AuthtokenType, bundle,
                        null, null, null);
                    PlatformPlugin.Logger.Verbose(null, "Waiting for the result");
                    Bundle userInfoBundle = (Bundle) result.Result;

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
