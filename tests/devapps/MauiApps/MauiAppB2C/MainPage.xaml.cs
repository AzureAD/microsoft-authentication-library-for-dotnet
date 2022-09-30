// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MauiB2C.MSALClient;
using Microsoft.Identity.Client;

namespace MauiB2C;

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
            // attempt silent login.
            // If this is very first time or user has signed out, it will throw MsalUiRequiredException
            AuthenticationResult result = await PCAWrapperB2C.Instance.AcquireTokenSilentAsync(B2CConstants.Scopes).ConfigureAwait(false);

            // show the IdToken
            await ShowMessage("AcquireTokenTokenSilent call IdToken", result.IdToken).ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            // This executes UI interaction to obtain token
            AuthenticationResult result = await PCAWrapperB2C.Instance.AcquireTokenInteractiveAsync(B2CConstants.Scopes).ConfigureAwait(false);

            // show the IdToken
            await ShowMessage("AcquireTokenInteractive call IdToken", result.IdToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ShowMessage("Exception in AcquireTokenTokenSilent", ex.Message).ConfigureAwait(false);
        }
    }
    private async void SignOutButton_Clicked(object sender, EventArgs e)
    {
        _ = await PCAWrapperB2C.Instance.SignOutAsync().ContinueWith(async (t) =>
        {
            await ShowMessage("Signed Out", "Sign out complete").ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    // display the message
    private Task ShowMessage(string title, string message)
    {
        _ = this.Dispatcher.Dispatch(async () =>
        {
            await DisplayAlert(title, message, "OK").ConfigureAwait(false);
        });

        return Task.CompletedTask;
    }
}

