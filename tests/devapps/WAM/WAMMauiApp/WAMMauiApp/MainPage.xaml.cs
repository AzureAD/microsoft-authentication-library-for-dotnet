// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace WAMMauiApp;

public partial class MainPage : ContentPage
{
    private static readonly IEnumerable<string> s_scopes = new[] { "user.read" };
    private static readonly string s_authority = "https://login.microsoftonline.com/common/";

    public MainPage()
	{
		InitializeComponent();
	}

	private void OnCounterClicked(object sender, EventArgs e)
	{
        var wamResult = CallIntoWamAsync();

        Result.Text = $"Account returned from WAM : {wamResult.Result} ";

		SemanticScreenReader.Announce(Result.Text);
	}

    private static async Task<string> CallIntoWamAsync()
    {
        var pca = CreatePublicClientForRuntime();
        
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
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        return result.Account.ToString();
    }
    private static IPublicClientApplication CreatePublicClientForRuntime()
    {
        var hwnd = ((MauiWinUIWindow)App.Current.Windows[0].Handler.PlatformView).WindowHandle;

        var pca = PublicClientApplicationBuilder.Create("4b0db8c2-9f26-4417-8bde-3f0e3656f8e0")
            .WithAuthority(s_authority)
            .WithParentActivityOrWindow(() => hwnd)
            //.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
            .WithLogging((x, y, z) => Console.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
            .Build();

        return pca;
    }
}

