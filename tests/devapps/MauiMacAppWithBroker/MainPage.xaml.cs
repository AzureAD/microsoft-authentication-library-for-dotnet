namespace MauiMacAppWithBroker;

using Microsoft.Maui.Controls;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private void AppendLog(string message)
	{
		string newEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}{Environment.NewLine}";
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			if (LogTextView.Text == null)
			{
				LogTextView.Text = string.Empty;
			}

			if (LogTextView.Text.Length + newEntry.Length > 10000)
			{
				int truncateIndex = LogTextView.Text.IndexOf(Environment.NewLine, LogTextView.Text.Length / 2);
				if (truncateIndex > 0)
				{
					LogTextView.Text = "...[Earlier logs truncated]..." + Environment.NewLine + 
						LogTextView.Text.Substring(truncateIndex + Environment.NewLine.Length);
				}
				else
				{
					LogTextView.Text = LogTextView.Text.Substring(LogTextView.Text.Length / 2);
				}
			}
			
			LogTextView.Text += newEntry;
			
			await Task.Delay(50).ConfigureAwait(false);
			
			await LogScrollView.ScrollToAsync(0, LogTextView.Height, true).ConfigureAwait(false);
		});
	}

	private async void OnACIACSClicked(object sender, EventArgs e)
	{
		// Disable button to prevent multiple clicks
		SetButtonEnabled(CACIACSBtn, false);
		
		SemanticScreenReader.Announce(CACIACSBtn.Text);

        PublicClientApplicationBuilder builder = PublicClientApplicationBuilder
			.Create("04b07795-8ddb-461a-bbee-02f9e1bf7b46") // Azure CLI client id
			.WithRedirectUri("msauth.com.msauth.unsignedapp://auth")
			.WithAuthority("https://login.microsoftonline.com/organizations");
		
		builder = builder.WithLogging(SampleLogging);

		builder = builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.OSX)
			{
				ListOperatingSystemAccounts = false,
				MsaPassthrough = false,
				Title = "MSAL Dev App .NET FX"
			}
		);

        IPublicClientApplication pca = builder.Build();
		
		try 
		{
			AppendLog("Starting interactive authentication...");
			AcquireTokenInteractiveParameterBuilder interactiveBuilder = pca.AcquireTokenInteractive(new string[]{"https://graph.microsoft.com/.default"});
			AuthenticationResult result = await interactiveBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
			AppendLog($"Interactive auth successful. User: {result.Account.Username}");

			IAccount account = result.Account;
			AppendLog("Starting silent authentication...");
			AcquireTokenSilentParameterBuilder silentBuilder = pca.AcquireTokenSilent(new string[]{"https://graph.microsoft.com/.default"}, account);
			// AcquireTokenSilentParameterBuilder silentBuilder = pca.AcquireTokenSilent(new string[]{"service::ssl.live.com::MBI_SSL"}, account);
			result = await silentBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
			AppendLog($"Silent auth successful. Access token length: {result.AccessToken.Length}");
		}
		catch (Exception ex)
		{
			AppendLog($"Error: {ex.Message}");
		}
		finally
		{
			// Always re-enable button when operation completes, even if there was an error
			SetButtonEnabled(CACIACSBtn, true);
		}
	}

	private async void OnGetAllAccountsClicked(object sender, EventArgs e)
	{
		// Disable button to prevent multiple clicks
		SetButtonEnabled(GetAllAccountsBtn, false);

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

        try 
		{
			AppendLog("Getting all accounts...");
			IEnumerable<IAccount> result = await pca.GetAccountsAsync().ConfigureAwait(false);
			IAccount[] array = result.ToArray();
			AppendLog($"Found {array.Length} accounts:");
			foreach (var account in array)
			{
				AppendLog($"- {account.Username}");
			}
		}
		catch (Exception ex)
		{
			AppendLog($"Error: {ex.Message}");
		}
		finally
		{
			// Always re-enable button when operation completes, even if there was an error
			SetButtonEnabled(GetAllAccountsBtn, true);
		}
	}

	// Method to handle the Clear Log button
	private void OnClearLogClicked(object sender, EventArgs e)
	{
		// Disable the button temporarily
		SetButtonEnabled(ClearLogBtn, false);
		
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			LogTextView.Text = string.Empty;
			AppendLog("Log cleared");
			
			// Ensure scroll view is at the top
			await LogScrollView.ScrollToAsync(0, 0, false).ConfigureAwait(false);
			
			// Re-enable the button
			SetButtonEnabled(ClearLogBtn, true);
		});
	}
	
	// Helper method to enable or disable a button on the UI thread
	private void SetButtonEnabled(Button button, bool enabled)
	{
		MainThread.BeginInvokeOnMainThread(() => 
		{
			button.IsEnabled = enabled;
		});
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



