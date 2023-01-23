// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Abstractions;

IIdentityLogger identityLogger = new IdentityLogger();

IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create("00bedee1-0e09-4a8d-81a0-0679c5a64a83")
                .WithExperimentalFeatures()
                .WithLogging(identityLogger, true)
                .Build();

string? scope = "https://management.azure.com";

do
{
    Console.WriteLine($"Acquiring token with scope {scope}");

    try
    {
        var result = await cca.AcquireTokenForClient(new string[] { scope })
            .WithManagedIdentity().ExecuteAsync().ConfigureAwait(false);

        Console.WriteLine("Success");
        Console.ReadLine();
    }
    catch (MsalServiceException e)
    {
        Console.WriteLine(e.ErrorCode);
        Console.WriteLine(e.Message);
        Console.WriteLine(e.StackTrace);
        Console.ReadLine();
    }

    Console.WriteLine("Enter the scope to acquire token.");
    scope = Console.ReadLine();
} while (!string.IsNullOrEmpty(scope));

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
