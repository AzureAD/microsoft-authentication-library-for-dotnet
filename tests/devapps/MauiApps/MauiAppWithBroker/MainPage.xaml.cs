// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MauiAppWithBroker.MSALClient;
using Microsoft.Identity.Client;

namespace MauiAppWithBroker
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        async private void OnSignInClicked(object sender, EventArgs e)
        {
            AuthenticationResult result = null;

            try
            {
                // attempt silent login.
                // If this is very first time and the device is not enrolled, it will throw MsalUiRequiredException
                // If the device is enrolled, this will succeed.
                result = await PCAWrapper.Instance.AcquireTokenSilentAsync(PCAWrapper.Scopes).ConfigureAwait(false);

                await ShowMessage("First AcquireTokenTokenSilent call", result.AccessToken).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                // This executes UI interaction ot obtain token
                result = await PCAWrapper.Instance.AcquireTokenInteractiveAsync(PCAWrapper.Scopes).ConfigureAwait(false);

                await ShowMessage("First AcquireTokenInteractive call", result.AccessToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ShowMessage("Exception in AcquireTokenTokenSilent", ex.Message).ConfigureAwait(false);
            }
        }

        // display the message
        private async Task ShowMessage(string title, string message)
        {
            _ = this.Dispatcher.Dispatch(async () =>
            {
                await DisplayAlert(title, message, "OK").ConfigureAwait(false);
            });
        }
    }
}
