// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.Identity.Client;
using UserDetailsClient.Core.Features.LogOn;

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
            var authResult = await B2CAuthenticationService.Instance.SignInAsync().ConfigureAwait(false);

            await ShowMessage("SignIn call", $"{authResult.AccessToken}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Checking the exception message 
            // should ONLY be done for B2C
            // reset and not any other error.
            if (ex.Message.Contains("AADB2C90118"))
                System.Console.WriteLine("Password reset");
            // Alert if any exception excluding user canceling sign-in dialog
            else
                await ShowMessage("Exception", ex.ToString()).ConfigureAwait(false);
        }
    }

    private async void SignOutButton_Clicked(object sender, EventArgs e)
    {
        _ = await B2CAuthenticationService.Instance.SignOutAsync().ContinueWith(async (t) =>
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

