#if WINDOWS_APP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.Security.Authentication.Web.Core;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal class WamWrapper : IDisposable
    {
        private readonly AuthenticationRequestParameters _requestParameters;

        public WamWrapper(AuthenticationRequestParameters requestParameters)
        {
            _requestParameters = requestParameters;
            AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested += OnAccountCommandsRequested;
        }

        private const string StoredAccountKey = "azureaccountid";

        private async void OnAccountCommandsRequested(
            AccountsSettingsPane sender,
            AccountsSettingsPaneCommandsRequestedEventArgs e)
        {
            // In order to make async calls within this callback, the deferral object is needed
            AccountsSettingsPaneEventDeferral deferral = e.GetDeferral();

            // This scenario only lets the user have one account at a time.
            // If there already is an account, we do not include a provider in the list
            // This will prevent the add account button from showing up.
            bool isPresent = ApplicationData.Current.LocalSettings.Values.ContainsKey(StoredAccountKey);

            if (isPresent)
            {
                await AddWebAccountAsync(e).ConfigureAwait(true);
            }
            else
            {
                await AddWebAccountProviderAsync(e).ConfigureAwait(true);
            }

            AddLinksAndDescription(e);

            deferral.Complete();
        }

        private async Task AddWebAccountProviderAsync(AccountsSettingsPaneCommandsRequestedEventArgs e)
        {
            // FindAccountProviderAsync returns the WebAccountProvider of an installed plugin
            // The Provider and Authority specifies the specific plugin
            // This scenario only supports Microsoft accounts.

            // The Microsoft account provider is always present in Windows 10 devices, as is the Azure AD plugin.
            // If a non-installed plugin or incorect identity is specified, FindAccountProviderAsync will return null   
            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                _requestParameters.AuthorityInfo.Host, "organizations");
                // "https://login.microsoft.com", "organizations");

            WebAccountProviderCommand providerCommand = new WebAccountProviderCommand(provider, WebAccountProviderCommandInvoked);
            e.WebAccountProviderCommands.Add(providerCommand);
        }

        private async Task AddWebAccountAsync(AccountsSettingsPaneCommandsRequestedEventArgs e)
        {
            WebAccountProvider provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                _requestParameters.AuthorityInfo.Host, "organizations");
                // "https://login.microsoft.com", "organizations");

            string accountID = (string)ApplicationData.Current.LocalSettings.Values[StoredAccountKey];
            WebAccount account = await WebAuthenticationCoreManager.FindAccountAsync(provider, accountID);

            if (account == null)
            {
                // The account has most likely been deleted in Windows settings
                // Unless there would be significant data loss, you should just delete the account
                // If there would be significant data loss, prompt the user to either re-add the account, or to remove it
                ApplicationData.Current.LocalSettings.Values.Remove(StoredAccountKey);
            }

            WebAccountCommand command = new WebAccountCommand(account, WebAccountInvoked, SupportedWebAccountActions.Remove);
            e.WebAccountCommands.Add(command);
        }

        private async void WebAccountProviderCommandInvoked(WebAccountProviderCommand command)
        {
            await RequestTokenAndSaveAccountAsync(
                command.WebAccountProvider,
                _requestParameters.Scope.AsSingleString(),
                _requestParameters.ClientId).ConfigureAwait(true);
        }

        private async void WebAccountInvoked(WebAccountCommand command, WebAccountInvokedArgs args)
        {
            if (args.Action == WebAccountAction.Remove)
            {
                await LogoffAndRemoveAccountAsync().ConfigureAwait(true);
            }
        }

        private async Task LogoffAndRemoveAccountAsync()
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(StoredAccountKey))
            {
                WebAccountProvider providertoDelete = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                _requestParameters.AuthorityInfo.Host, "organizations");
                // "https://login.microsoft.com", "organizations");

                WebAccount accountToDelete = await WebAuthenticationCoreManager.FindAccountAsync(
                    providertoDelete,
                    (string)ApplicationData.Current.LocalSettings.Values[StoredAccountKey]);

                if (accountToDelete != null)
                {
                    await accountToDelete.SignOutAsync();
                }

                ApplicationData.Current.LocalSettings.Values.Remove(StoredAccountKey);
            }
        }

        private async Task RequestTokenAndSaveAccountAsync(WebAccountProvider provider, string scope, string clientId)
        {
            try
            {
                WebTokenRequest webTokenRequest = new WebTokenRequest(provider, scope, clientId);
                webTokenRequest.Properties.Add("resource", "https://graph.microsoft.com");

                // If the user selected a specific account, RequestTokenAsync will return a token for that account.
                // The user may be prompted for credentials or to authorize using that account with your app
                // If the user selected a provider, the user will be prompted for credentials to login to a new account
                WebTokenRequestResult webTokenRequestResult = await WebAuthenticationCoreManager.RequestTokenAsync(webTokenRequest);

                // If a token was successfully returned, then store the WebAccount Id into local app data
                // This Id can be used to retrieve the account whenever needed. To later get a token with that account
                // First retrieve the account with FindAccountAsync, and include that webaccount 
                // as a parameter to RequestTokenAsync or RequestTokenSilentlyAsync
                if (webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.Success)
                {
                    ApplicationData.Current.LocalSettings.Values.Remove(StoredAccountKey);
                    ApplicationData.Current.LocalSettings.Values[StoredAccountKey] = webTokenRequestResult.ResponseData[0].WebAccount.Id;
                }

                //OutputTokenResult(webTokenRequestResult);
            }
            catch (Exception ex)
            {
                //rootPage.NotifyUser("Web Token request failed: " + ex.Message, NotifyType.ErrorMessage);
            }
        }

        private void AddLinksAndDescription(AccountsSettingsPaneCommandsRequestedEventArgs e)
        {
            e.HeaderText = "Describe what adding an account to your application will do for the user";

            // You can add links such as privacy policy, help, general account settings
            //e.Commands.Add(new SettingsCommand("privacypolicy", "Privacy policy", PrivacyPolicyInvoked));
            //e.Commands.Add(new SettingsCommand("otherlink", "Other link", OtherLinkInvoked));
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested -= OnAccountCommandsRequested;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WamWrapper()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
#endif
