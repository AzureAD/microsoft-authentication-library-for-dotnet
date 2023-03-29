// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System.Runtime.InteropServices;
using Windows.Media.Streaming.Adaptive;

namespace WAMMauiApp;

public partial class MainPage : ContentPage
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();
    private static readonly IEnumerable<string> s_scopes = new[] { "user.read" };
    private static readonly string s_authority = "https://login.microsoftonline.com/common/";

    public MainPage()
	{
		InitializeComponent();
	}

	private void OnCounterClicked(object sender, EventArgs e)
	{
        var wamResult = CallIntoWamAsync();

        CounterBtn.Text = $"Account returned from WAM : {wamResult.Result} ";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}

    private static async Task<string> CallIntoWamAsync()
    {
        var pca = CreatePublicClientForRuntime();
        IntPtr hWnd = GetConsoleWindow();

        IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true);
        var acc = accounts.FirstOrDefault();

        AuthenticationResult result = null;
        try
        {
            result = await pca
                .AcquireTokenSilent(s_scopes, acc)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithParentActivityOrWindow(hWnd)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        return result.Account.ToString();
    }
    private static IPublicClientApplication CreatePublicClientForRuntime()
    {
        IntPtr hWnd = GetConsoleWindow();

        var pca = PublicClientApplicationBuilder.Create("4b0db8c2-9f26-4417-8bde-3f0e3656f8e0")
            .WithAuthority(s_authority)
            .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
            .WithLogging((x, y, z) => Console.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
            .Build();

        return pca;
    }
}

