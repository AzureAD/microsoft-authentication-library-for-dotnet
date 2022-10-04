// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MauiAppBasic.MSALClient;
using Microsoft.Identity.Client;

namespace MauiAppBasic
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnSignInClicked(object sender, EventArgs e)
        {
            try
            {
                PCAWrapper.Instance.UseEmbedded = this.useEmbedded.IsChecked;
                // attempt silent login.
                // If this is very first time or user has signed out, it will throw MsalUiRequiredException
                AuthenticationResult result = await PCAWrapper.Instance.AcquireTokenSilentAsync(PCAWrapper.Scopes).ConfigureAwait(false);

                // call Web API to get the data
                string data = await CallWebAPIWithToken(result).ConfigureAwait(false);

                // show the data
                await ShowMessage("AcquireTokenTokenSilent call", data).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                // This executes UI interaction to obtain token
                AuthenticationResult result = await PCAWrapper.Instance.AcquireTokenInteractiveAsync(PCAWrapper.Scopes).ConfigureAwait(false);

                // call Web API to get the data
                string data = await CallWebAPIWithToken(result).ConfigureAwait(false);

                // show the data
                await ShowMessage("AcquireTokenInteractive call", data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ShowMessage("Exception in AcquireTokenTokenSilent", ex.Message).ConfigureAwait(false);
            }
        }
        private async void SignOutButton_Clicked(object sender, EventArgs e)
        {
            _ = await PCAWrapper.Instance.SignOutAsync().ContinueWith(async (t) =>
            {
                await ShowMessage("Signed Out", "Sign out complete").ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        // Call the web api. The code is left in the Ux file for easy to see.
        private async Task<string> CallWebAPIWithToken(AuthenticationResult authResult)
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
        private async Task ShowMessage(string title, string message)
        {
            _ = this.Dispatcher.Dispatch(async () =>
            {
                await DisplayAlert(title, message, "OK").ConfigureAwait(false);
            });
        }

    }
}
