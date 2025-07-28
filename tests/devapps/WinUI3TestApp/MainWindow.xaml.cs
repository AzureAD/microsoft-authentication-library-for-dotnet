// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace WinUI3TestApp;

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
            .WithAuthority("https://login.microsoftonline.com/common")
            .WithRedirectUri("http://localhost")
            .Build();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ResultTextBlock.Text = "Authenticating...";
            
            var result = await _pca.AcquireTokenInteractive(new[] { "User.Read" })
                .WithParentActivityOrWindow(this) // Pass the WinUI3 window
                .ExecuteAsync()
                .ConfigureAwait(false);

            ResultTextBlock.Text = $"Success! User: {result.Account.Username}";
        }
        catch (Exception ex)
        {
            ResultTextBlock.Text = $"Error: {ex.Message}";
        }
    }
}
