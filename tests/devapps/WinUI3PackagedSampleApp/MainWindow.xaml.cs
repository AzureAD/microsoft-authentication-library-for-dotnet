// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI3PackagedSampleApp;

public sealed partial class MainWindow : Window
{
    private IPublicClientApplication _pca;

    public MainWindow()
    {
        this.InitializeComponent();
        InitializeMsal();
    }

    private void InitializeMsal()
    {
        _pca = PublicClientApplicationBuilder
            .Create("9c0cb93c-2744-45ee-8399-81688062c058")
            .WithAuthority("https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47")
            .WithRedirectUri("http://localhost")
            .WithWindowsEmbeddedBrowserSupport()
            .Build();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool useEmbeddedWebView = UseEmbeddedWebViewCheckBox.IsChecked == true;
            string authMethod = useEmbeddedWebView ? "Embedded WebView" : "System Browser";
            
            ResultTextBlock.Text = $"Authenticating using {authMethod}...";

            var authBuilder = _pca.AcquireTokenInteractive(new[] { "user.read" });

            if (useEmbeddedWebView)
            {
                // Use embedded WebView
                authBuilder = authBuilder.WithUseEmbeddedWebView(true)
                                           .WithParentActivityOrWindow(this); // Pass the WinUI3 window if needed
            }
            else
            {
                // Use system browser (default behavior)
                authBuilder = authBuilder.WithUseEmbeddedWebView(false);
            }

            var result = await authBuilder
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Display authentication success and result details
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ResultTextBlock.Text = $"🎉 Authentication Successful! (via {authMethod})\n\n" +
                                     $"User: {result.Account.Username}\n" +
                                     $"Display Name: {result.Account.GetTenantProfiles()?.FirstOrDefault()?.ClaimsPrincipal?.FindFirst("name")?.Value ?? "N/A"}\n" +
                                     $"Tenant ID: {result.TenantId}\n" +
                                     $"Account ID: {result.Account.HomeAccountId?.Identifier}\n" +
                                     $"Token Type: {result.TokenType}\n" +
                                     $"Expires On: {result.ExpiresOn:yyyy-MM-dd HH:mm:ss}\n" +
                                     $"Scopes: {string.Join(", ", result.Scopes)}\n" +
                                     $"Access Token (first 50 chars): {result.AccessToken.Substring(0, Math.Min(50, result.AccessToken.Length))}...\n" +
                                     $"Correlation ID: {result.CorrelationId}";
            });
        }
        catch (Exception ex)
        {
            string authMethod = UseEmbeddedWebViewCheckBox.IsChecked == true ? "Embedded WebView" : "System Browser";
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ResultTextBlock.Text = $"Authentication Failed! (via {authMethod})\n\nError: {ex.Message}\n\nException Type: {ex.GetType().Name}";
            });
        }
    }
}
