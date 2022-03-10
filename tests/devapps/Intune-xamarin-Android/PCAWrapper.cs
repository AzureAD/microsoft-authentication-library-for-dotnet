// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Identity.Client;

namespace Intune_xamarin_Android
{
    /// <summary>
    /// This is a wrapper for PCA. It is singleton and can be utilized by both application and the MAM callback
    /// </summary>
    public class PCAWrapper
    {

        /// <summary>
        /// This is the singleton used by consumers
        /// </summary>
        static internal PCAWrapper Instance { get; }

        internal IPublicClientApplication PCA { get; }

        /// <summary>
        /// The authority for the MSAL PublicClientApplication. Sign in will use this URL.
        /// </summary>
        private const string _authority = "https://login.microsoftonline.com/organizations";

        // ClientID of the application
        private static string _clientID = "6d50af5d-2529-4ff4-912f-c1d6ad06953e"; // TODO - Replace with your client Id. And also replace in the AndroidManifest.xml

        // redirect URI for the application
        private static string _redirectURI = $"msauth://com.sameerk.intune.test.xamarin/EHyvOdXj4uLXJXDaOMy5lwANmp0="; // TODO - Replace with your redirectURI

        // TenantID of your organization
        private static string _tenantID = "7257a09f-53cc-4a91-aca8-0cb6713642a5"; // TODO - Replace with your TenantID. And also replace in the AndroidManifest.xml

        static string[] clientCapabilities = { "ProtApp" }; // It is must to have these capabilities

        // private constructor for singleton
        private PCAWrapper()
        {
            // Create PCA once. Make sure that all the config parameters below are passed
            // ClientCapabilities - must have ProtApp
            PCA = PublicClientApplicationBuilder
                .Create(_clientID)
                .WithAuthority(_authority)
                .WithBroker()
                .WithClientCapabilities(clientCapabilities)
                .WithTenantId(_tenantID)
                .WithRedirectUri(_redirectURI)
                .Build();
        }

        /// <summary>
        /// Static constructor to instantiate PCA
        /// </summary>
        static PCAWrapper()
        {
            Instance = new PCAWrapper();
        }

        /// <summary>
        /// Perform the intractive acquistion of the token for the given scope
        /// </summary>
        /// <param name="scopes">Scope</param>
        /// <param name="parentWindow">Parent window</param>
        /// <returns></returns>
        internal async Task<AuthenticationResult> DoInteractiveAsync(string[] scopes, Activity parentWindow)
        {
            return await PCA.AcquireTokenInteractive(scopes)
                                    .WithParentActivityOrWindow(parentWindow)
                                    .WithUseEmbeddedWebView(true)
                                    .ExecuteAsync()
                                    .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquire the token silently
        /// </summary>
        /// <param name="scopes">scopes</param>
        /// <returns>Authenticaiton result</returns>
        internal async Task<AuthenticationResult> DoSilentAsync(string[] scopes)
        {
            if (PCA == null)
            {
                return null;
            }

            var accts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            var acct = accts.FirstOrDefault();
            if (acct != null)
            {
                var silentParamBuilder = PCA.AcquireTokenSilent(scopes, acct);
                var authResult = await silentParamBuilder
                                            .ExecuteAsync().ConfigureAwait(false);
                return authResult;
            }
            else
            {
                throw new MsalUiRequiredException("ErrCode", "ErrMessage");
            }
        }

        /// <summary>
        /// Signout may not perform the complete signout as company portal may hold
        /// the token.
        /// </summary>
        /// <returns></returns>
        internal async Task SignOut()
        {
            var accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            while (accounts.Any())
            {
                var acct = accounts.FirstOrDefault();
                await PCA.RemoveAsync(acct).ConfigureAwait(false);
                accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            }
        }
    }
}
