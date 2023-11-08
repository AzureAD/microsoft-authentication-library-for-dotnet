// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;

int lineWidth = 50; // Adjust the width as needed
string line = new ('-', lineWidth);

IIdentityLogger identityLogger = new IdentityLogger();

IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithLogging(identityLogger, true)
                .WithExperimentalFeatures(true)
                .WithClientCapabilities(new[] { "cp1" })
                .Build();

string? scope = "https://management.azure.com";

do
{
    Console.WriteLine($"Acquiring token with scope {scope}");

    try
    {
        for (int i = 1; i <= 3; i++)
        {
            AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(scope)
                .WithForceRefresh(i == 3)
                .WithClaims("{\"code\":\"red\", \"max\":30, \"min\":null}")
                .ExecuteAsync().ConfigureAwait(false);

            Console.WriteLine(line);
            string tokenSource = result.AuthenticationResultMetadata.TokenSource.ToString();
            if ((i == 1 || i == 3) && tokenSource == "IdentityProvider")
            {
                PrintSuccess(i, tokenSource);
            }
            else if (i == 2 && tokenSource == "Cache")
            {
                PrintSuccess(i, tokenSource);
            }
            else
            {
                PrintFailure(i, tokenSource);
            }
            Console.WriteLine(line);
        }

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
