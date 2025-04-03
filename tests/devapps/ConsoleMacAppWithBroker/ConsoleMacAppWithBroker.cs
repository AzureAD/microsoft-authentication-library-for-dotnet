﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

class ConsoleMacAppWithBroker
{

    static async Task Main(string[] args)
    {

        PublicClientApplicationBuilder builder = PublicClientApplicationBuilder
            .Create("7e5e53b7-864a-40bc-903f-89708a6af755")
            .WithRedirectUri("msauth.com.msauth.unsignedapp://auth")
            //.WithRedirectUri("http://localhost")
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

        AcquireTokenInteractiveParameterBuilder interactiveBuilder = pca.AcquireTokenInteractive(new string[] { "https://graph.microsoft.com/.default" });


        AuthenticationResult result = await interactiveBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

        IAccount account = result.Account;

        // AcquireTokenSilentParameterBuilder silentBuilder = pca.AcquireTokenSilent(new string[] { "https://graph.microsoft.com/.default" }, account);

        // result = await silentBuilder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine($"Access token: {result.AccessToken}");
        Console.WriteLine($"Expires on: {result.ExpiresOn}");

    }

	private static void SampleLogging(LogLevel level, string message, bool containsPii)
	{
		try
        {
			string homeDirectory = Environment.GetEnvironmentVariable("HOME");
            string filePath = Path.Combine(homeDirectory, "msalnet.log");
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


