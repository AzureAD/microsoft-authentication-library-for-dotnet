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
            try
            {
                // First attempt silent login, which checks the cache for an existing valid token.
                // If this is very first time or user has signed out, it will throw MsalUiRequiredException
                AuthenticationResult result = await PCAWrapper.Instance.AcquireTokenSilentAsync(AppConstants.Scopes).ConfigureAwait(false);

                // call Web API to get the data
                string data = await CallWebAPIWithTokenAsync(result).ConfigureAwait(false);

                // show the data
                await ShowMessageAsync("AcquireTokenTokenSilent call", data).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                // This executes UI interaction to obtain token
                AuthenticationResult result = await PCAWrapper.Instance.AcquireTokenInteractiveAsync(AppConstants.Scopes).ConfigureAwait(false);
                // call Web API to get the data
                string data = await CallWebAPIWithTokenAsync(result).ConfigureAwait(false);

                // show the data
                await ShowMessageAsync("AcquireTokenInteractive call", data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Exception in AcquireTokenTokenSilent", ex.Message).ConfigureAwait(false);
            }
        }

        private async void SignOutButton_Clicked(object sender, EventArgs e)
        {
            await PCAWrapper.Instance.SignOutAsync().ContinueWith(async (t) =>
            {
                await ShowMessageAsync("Signed Out", "Sign out complete").ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        // Call the web api. The code is left in the Ux file for easy to see.
        private async Task<string> CallWebAPIWithTokenAsync(AuthenticationResult authResult)
        {
            try
            {
                //get data from API
                HttpClient client = new HttpClient();
                // create the request
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");

                // ** Add Authorization Header **
                message.Headers.Add("Authorization", authResult.CreateAuthorizationHeader());

                // send the request and return the response
                HttpResponseMessage response = await client.SendAsync(message).ConfigureAwait(false);
                string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return responseString;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        // display the message
        private async Task ShowMessageAsync(string title, string message)
        {
            _ = this.Dispatcher.Dispatch(async () =>
            {
                await DisplayAlert(title, message, "OK").ConfigureAwait(false);
            });
        }
    }
}
