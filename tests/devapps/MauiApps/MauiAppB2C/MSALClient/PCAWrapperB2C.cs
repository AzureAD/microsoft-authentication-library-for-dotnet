// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;

namespace MauiB2C.MSALClient
{
    public class PCAWrapperB2C
    {
        /// <summary>
        /// This is the singleton used by consumers
        /// </summary>
        public static PCAWrapperB2C Instance { get; private set; } = new PCAWrapperB2C();

        internal IPublicClientApplication PCA { get; }

        // private constructor for singleton
        private PCAWrapperB2C()
        {
            // Create PCA once. Make sure that all the config parameters below are passed
            PCA = PublicClientApplicationBuilder
                                        .Create(B2CConstants.ClientID)
                                        .WithB2CAuthority(B2CConstants.AuthoritySignInSignUp)
                                        .WithIosKeychainSecurityGroup(B2CConstants.IOSKeyChainGroup)
                                        .WithRedirectUri($"msal{B2CConstants.ClientID}://auth")
                                        .Build();
        }

        /// <summary>
        /// Acquire the token silently
        /// </summary>
        /// <param name="scopes">desired scopes</param>
        /// <returns>Authenticaiton result</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scopes)
        {
            var accts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            IEnumerable<IAccount> accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);

            AuthenticationResult authResult = await PCA.AcquireTokenSilent(scopes, GetAccountByPolicy(accounts, B2CConstants.PolicySignUpSignIn))
               .WithB2CAuthority(B2CConstants.AuthoritySignInSignUp)
               .ExecuteAsync()
               .ConfigureAwait(false);

            return authResult;
        }

        /// <summary>
        /// Perform the intractive acquistion of the token for the given scope
        /// </summary>
        /// <param name="scopes">desired scopes</param>
        /// <returns></returns>
        internal async Task<AuthenticationResult> AcquireTokenInteractiveAsync(string[] scopes)
        {
            SystemWebViewOptions systemWebViewOptions = new SystemWebViewOptions();
#if IOS
            // Hide the privacy prompt in iOS
            systemWebViewOptions.iOSHidePrivacyPrompt = true;
#endif

            return await PCA.AcquireTokenInteractive(B2CConstants.Scopes)
                                                        .WithSystemWebViewOptions(systemWebViewOptions)
                                                        .WithParentActivityOrWindow(PlatformConfigImpl.Instance.ParentWindow)
                                                        .ExecuteAsync()
                                                        .ConfigureAwait(false);
        }

        /// <summary>
        /// Signout may not perform the complete signout as company portal may hold
        /// the token.
        /// </summary>
        /// <returns></returns>
        internal async Task SignOutAsync()
        {
            var accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            foreach (var acct in accounts)
            {
                await PCA.RemoveAsync(acct).ConfigureAwait(false);
            }
        }

        private IAccount GetAccountByPolicy(IEnumerable<IAccount> accounts, string policy)
        {
            foreach (var account in accounts)
            {
                string userIdentifier = account.HomeAccountId.ObjectId.Split('.')[0];
                if (userIdentifier.EndsWith(policy.ToLower()))
                    return account;
            }

            return null;
        }
    }
}
