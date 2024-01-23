// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;

int lineWidth = 50; // Adjust the width as needed
string line = new('-', lineWidth);

Console.WriteLine(ManagedIdentityApplication.GetBindingCertificate());

IIdentityLogger identityLogger = new IdentityLogger();

IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder
                //.Create(ManagedIdentityId.SystemAssigned)
                .Create(ManagedIdentityId.WithUserAssignedClientId("3b57c42c-3201-4295-ae27-d6baec5b7027"))
                .WithLogging(identityLogger, true)
                .WithExperimentalFeatures(true)
                .WithClientCapabilities(new[] { "cp1" })
                .Build();

Console.WriteLine(mi.IsProofOfPossessionSupportedByClient());

Console.WriteLine(mi.IsClaimsSupportedByClient());

string? scope = "https://management.azure.com";

do
{
    Console.WriteLine($"Acquiring token with scope {scope}");

    try
    {
        Console.WriteLine(line);

        //Basic Token Request 
        AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(scope)
            .ExecuteAsync().ConfigureAwait(false);

        int testNumber = 1;
        string tokenSource = result.AuthenticationResultMetadata.TokenSource.ToString();

        PrintResult(testNumber, tokenSource, "IdentityProvider");
        Console.WriteLine(line);

        //Second Token Request  must be from Cache 
        result = await mi.AcquireTokenForManagedIdentity(scope)
            .ExecuteAsync().ConfigureAwait(false);

        testNumber = 2;
        tokenSource = result.AuthenticationResultMetadata.TokenSource.ToString();

        PrintResult(testNumber, tokenSource, "Cache");
        Console.WriteLine(line);

        //Third Token Request must be from IDP (claims)
        result = await mi.AcquireTokenForManagedIdentity(scope)
            .WithClaims("{\"code\":\"red\", \"max\":30, \"min\":null}")
            .ExecuteAsync().ConfigureAwait(false);

        testNumber = 3;
        tokenSource = result.AuthenticationResultMetadata.TokenSource.ToString();

        PrintResult(testNumber, tokenSource, "IdentityProvider");
        Console.WriteLine(line);

        //Fourth Token Request must be from Cache
        result = await mi.AcquireTokenForManagedIdentity(scope)
            .ExecuteAsync().ConfigureAwait(false);

        testNumber = 4;
        tokenSource = result.AuthenticationResultMetadata.TokenSource.ToString();

        PrintResult(testNumber, tokenSource, "Cache");
        Console.WriteLine(line);

        //Fifth Token Request must be from IdentityProvider (Force Refresh)
        result = await mi.AcquireTokenForManagedIdentity(scope)
            .WithForceRefresh(true)
            .ExecuteAsync().ConfigureAwait(false);

        testNumber = 5;
        tokenSource = result.AuthenticationResultMetadata.TokenSource.ToString();

        PrintResult(testNumber, tokenSource, "IdentityProvider");
        Console.WriteLine(line);

        Console.ReadLine();
    }
    catch (MsalServiceException e)
    {
        Console.ForegroundColor = ConsoleColor.Red; // Set text color to red for errors
        Console.WriteLine(e.ErrorCode);
        Console.WriteLine(e.Message);
        Console.WriteLine(e.StackTrace);
        Console.ResetColor(); // Reset text color to the default
        Console.ReadLine();
    }

    Console.WriteLine("Enter the scope to acquire token.");
    scope = Console.ReadLine();
} while (!string.IsNullOrEmpty(scope));

static void PrintSuccess(int i, string tokenSource)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"result {i} - Token Source: {tokenSource}");
    Console.WriteLine($"result {i} Success");
    Console.ResetColor(); // Reset text color to the default
}

static void PrintResult(int testNumber, string tokenSource, string expectedTokenSource)
{
    if (tokenSource == expectedTokenSource)
    {
        PrintSuccess(testNumber, tokenSource);
    }
    else
    {
        PrintFailure(testNumber, tokenSource);
    }
}

static void PrintFailure(int i, string tokenSource)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"result {i} - Token Source: {tokenSource}");
    Console.WriteLine($"result {i} Failure");
    Console.ResetColor(); // Reset text color to the default
}

class IdentityLogger : IIdentityLogger
{
    public EventLogLevel MinLogLevel { get; }

    public IdentityLogger()
    {
        MinLogLevel = EventLogLevel.Verbose;
    }

    public bool IsEnabled(EventLogLevel eventLogLevel)
    {
        return eventLogLevel <= MinLogLevel;
    }

    public void Log(LogEntry entry)
    {
        //Log Message here:
        Console.WriteLine(entry.Message);
    }
}
