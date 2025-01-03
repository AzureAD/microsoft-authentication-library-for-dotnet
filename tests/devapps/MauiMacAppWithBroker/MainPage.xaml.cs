namespace MauiMacAppWithBroker;


using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System;
using System.Runtime.InteropServices;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnCounterClicked(object sender, EventArgs e)
	{

		SemanticScreenReader.Announce(CounterBtn.Text);

		var builder = PublicClientApplicationBuilder
			.Create("04b07795-8ddb-461a-bbee-02f9e1bf7b46")
			.WithRedirectUri("msauth.com.msauth.unsignedapp://auth")
			.WithClientCapabilities(new[] { "cp1" })
			.WithAuthority("https://login.microsoftonline.com/common");
		
		builder = builder.WithLogging(SampleLogging);

		builder = builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Mac)
			{
				ListOperatingSystemAccounts = false,
				MsaPassthrough = false,
				Title = "MSAL Dev App .NET FX"
			}
		);
		
		var pca = builder.Build();

		LogCallback logCallback = null;
		LogLevel level;

		
		RunAtiAsync(pca);
	}

	private async Task<AuthenticationResult> RunAtiAsync(IPublicClientApplication pca)
	{
		var builder = pca.AcquireTokenInteractive(new string[]{"https://graph.microsoft.com/.default"});

		builder.WithUseEmbeddedWebView(true)
		//.WithExtraQueryParameters("domain_hint=live.com") -- will force AAD login with browser
		//.WithExtraQueryParameters("msafed=0")             -- will force MSA login with browser
		.WithEmbeddedWebViewOptions(
		new EmbeddedWebViewOptions()
		{
			Title = "Hello world",
		});

		AuthenticationResult result = await builder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
		return result;
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



