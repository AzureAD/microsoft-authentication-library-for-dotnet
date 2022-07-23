using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// using Xamarin.Forms;

namespace UserDetailsClient.Core.Features.LogOn
{
    /// <summary>
    ///  For simplicity, we'll have this as a singleton. 
    /// </summary>
    public class B2CAuthenticationService
    {
        private readonly IPublicClientApplication _pca;


        private static readonly Lazy<B2CAuthenticationService> lazy = new Lazy<B2CAuthenticationService>
           (() => new B2CAuthenticationService());

        public static B2CAuthenticationService Instance { get { return lazy.Value; } }

        private B2CAuthenticationService()
        {
            // default redirectURI; each platform specific project will have to override it with its own
            var builder = PublicClientApplicationBuilder
                                                .Create(B2CConstants.ClientID)
                                                .WithB2CAuthority(B2CConstants.AuthoritySignInSignUp)
                                                .WithIosKeychainSecurityGroup(B2CConstants.IOSKeyChainGroup)
                                                .WithRedirectUri($"msal{B2CConstants.ClientID}://auth");

            _pca = builder.Build();
        }

        public async Task<AuthenticationResult> SignInAsync()
        {
            AuthenticationResult authResult;
            try
            {
                // acquire token silent
                authResult = await AcquireTokenSilent().ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                // acquire token interactive
                authResult = await SignInInteractively().ConfigureAwait(false);
            }
            return authResult;
        }

        private async Task<AuthenticationResult> AcquireTokenSilent()
        {
            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            AuthenticationResult authResult = await _pca.AcquireTokenSilent(B2CConstants.Scopes, GetAccountByPolicy(accounts, B2CConstants.PolicySignUpSignIn))
               .WithB2CAuthority(B2CConstants.AuthoritySignInSignUp)   
               .ExecuteAsync()
               .ConfigureAwait(false);

            return authResult;
        }

        private async Task<AuthenticationResult> SignInInteractively()
        {
            // Hide the privacy prompt
            SystemWebViewOptions systemWebViewOptions = new SystemWebViewOptions()
            {
                iOSHidePrivacyPrompt = true,
            };

            AuthenticationResult authResult = null;
            // Android implementation is based on https://github.com/jamesmontemagno/CurrentActivityPlugin
            // iOS implementation would require to expose the current ViewControler - not currently implemented as it is not required
            // UWP does not require this
            var parentWindow = DependencyService.Get<IParentWindowLocatorService>()?.GetCurrentParentWindow();

            authResult = await _pca.AcquireTokenInteractive(B2CConstants.Scopes)
                                                        .WithSystemWebViewOptions(systemWebViewOptions)
                                                        .WithParentActivityOrWindow(parentWindow)
                                                        .ExecuteAsync()
                                                        .ConfigureAwait(false);

            return authResult;
        }

        public async Task SignOutAsync()
        {

            IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            while (accounts.Any())
            {
                await _pca.RemoveAsync(accounts.FirstOrDefault()).ConfigureAwait(false);
                accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            }
        }

        private IAccount GetAccountByPolicy(IEnumerable<IAccount> accounts, string policy)
        {
            foreach (var account in accounts)
            {
                string userIdentifier = account.HomeAccountId.ObjectId.Split('.')[0];
                if (userIdentifier.EndsWith(policy.ToLower())) return account;
            }

            return null;
        }
    }
}
