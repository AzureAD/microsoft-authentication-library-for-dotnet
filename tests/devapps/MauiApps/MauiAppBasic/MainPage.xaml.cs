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
            AuthenticationResult result = null;

            try
            {
                PCAWrapper.Instance.UseEmbedded = this.useEmbedded.IsChecked;
                // attempt silent login.
                result = await PCAWrapper.Instance.AcquireTokenSilentAsync(PCAWrapper.Scopes).ConfigureAwait(false);

                await ShowMessage("First AcquireTokenTokenSilent call", result.AccessToken)
                                .ContinueWith(async (_) =>
                                {
                                    string data = await CallWebAPIWithToken(result).ConfigureAwait(false);

                                    await ShowMessage("Data after passing token", data).ConfigureAwait(false);
                                }).ConfigureAwait(false);

            }
            catch (MsalUiRequiredException)
            {
                // This executes UI interaction ot obtain token
                result = await PCAWrapper.Instance.AcquireTokenInteractiveAsync(PCAWrapper.Scopes).ConfigureAwait(false);

                await ShowMessage("First AcquireTokenInteractive call", result.AccessToken)
                                .ContinueWith(async (_) =>
                                {
                                    string data = await CallWebAPIWithToken(result).ConfigureAwait(false);

                                    await ShowMessage("Data after passing token", data).ConfigureAwait(false);
                                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ShowMessage("Exception in AcquireTokenTokenSilent", ex.Message).ConfigureAwait(false);
            }
        }

        private async Task<string> CallWebAPIWithToken(AuthenticationResult authResult)
        {
            try
            {
                //get data from API
                HttpClient client = new HttpClient();
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
                message.Headers.Add("Authorization", authResult.CreateAuthorizationHeader());
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

        private async void SignOutButton_Clicked(object sender, EventArgs e)
        {
            _= await PCAWrapper.Instance.SignOut().ContinueWith(async (t) =>
                     {
                        await ShowMessage("Signed Out", "Sign out complete").ConfigureAwait(false);
                     }).ConfigureAwait(false);
        }
    }
}
