// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System.Runtime.InteropServices;

[DllImport("kernel32.dll")]
static extern IntPtr GetConsoleWindow();

IEnumerable<string> s_scopes = new[] { "user.read" };
string s_authority = "https://login.microsoftonline.com/common/";
IntPtr hWnd = GetConsoleWindow();

var pca = PublicClientApplicationBuilder.Create("4b0db8c2-9f26-4417-8bde-3f0e3656f8e0")
        .WithAuthority(s_authority)
        .WithBrokerPreview(true)
        .WithLogging((x, y, z) => Console.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
        .Build();

IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(true);
var acc = accounts.FirstOrDefault();
AuthenticationResult? result = null;

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

Console.WriteLine(result.AccessToken);
Console.Read();

