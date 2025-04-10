namespace MauiMacAppWithBroker;

using Microsoft.Maui.Controls;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System;
using System.Runtime.InteropServices;

public partial class MainPage : ContentPage
{
	private bool inUse = false;

	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnACIACSClicked(object sender, EventArgs e)
	{
		
		if (inUse)
		{
			await Application.Current.MainPage.DisplayAlert("Warning", "Please wait until previous operation finished", "OK").ConfigureAwait(false);
			return;
		}
		inUse = true;

		SemanticScreenReader.Announce(CACIACSBtn.Text);

        PublicClientApplicationBuilder builder = PublicClientApplicationBuilder
			.Create("7e5e53b7-864a-40bc-903f-89708a6af755")
			.WithRedirectUri("msauth.com.msauth.unsignedapp://auth")
			.WithAuthority("https://login.microsoftonline.com/common");
		
		builder = builder.WithLogging(SampleLogging);

		builder = builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.OSX)
			{
				ListOperatingSystemAccounts = false,
				MsaPassthrough = false,
				Title = "MSAL Dev App .NET FX"
			}
		);

        IPublicClientApplication pca = builder.Build();

        AcquireTokenInteractiveParameterBuilder interactiveBuilder = pca.AcquireTokenInteractive(new string[]{"https://graph.microsoft.com/.default"});

		
		AuthenticationResult result = await interactiveBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

		IAccount account = result.Account;

        AcquireTokenSilentParameterBuilder silentBuilder = pca.AcquireTokenSilent(new string[]{"https://graph.microsoft.com/.default"}, account);

		result = await silentBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
		

		inUse = false;
	}

	private async void OnGetAllAccountsClicked(object sender, EventArgs e)
	{
		if (inUse)
		{
			await Application.Current.MainPage.DisplayAlert("Warning", "Please wait until previous operation finished", "OK").ConfigureAwait(false);
			return;
		}
		inUse = true;

		PublicClientApplicationBuilder builder = PublicClientApplicationBuilder
			.Create("7e5e53b7-864a-40bc-903f-89708a6af755")
			.WithRedirectUri("msauth.com.msauth.unsignedapp://auth")
			.WithAuthority("https://login.microsoftonline.com/common");
		
		builder = builder.WithLogging(SampleLogging);

		builder = builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.OSX)
			{
				ListOperatingSystemAccounts = false,
				MsaPassthrough = false,
				Title = "MSAL Dev App .NET FX"
			}
		);
        IPublicClientApplication pca = builder.Build();

        System.Runtime.CompilerServices.ConfiguredTaskAwaitable<IEnumerable<IAccount>> accounts = pca.GetAccountsAsync().ConfigureAwait(false);
		IEnumerable<IAccount> result = await accounts;
		IAccount[] array = result.ToArray();
		

		inUse = false;
	}


	private static void SampleLogging(LogLevel level, string message, bool containsPii)
	{
		try
        {
			string homeDirectory = Environment.GetEnvironmentVariable("HOME");
            string filePath = Path.Combine(homeDirectory, "msalnet.log");
			// An example log path could be: /Users/fengga/Library/Containers/com.microsoft.mauimacappwithbroker/Data/msalnet.log
			using (StreamWriter writer = new StreamWriter(filePath, append: true))
			{
				writer.WriteLine($"{level} {message}");
			}
		}
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write log: {ex.Message}");
        }
	}

}



