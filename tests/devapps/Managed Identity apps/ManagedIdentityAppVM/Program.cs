// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Identity.Client.MtlsPop;

IIdentityLogger identityLogger = new IdentityLogger();

IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithLogging(identityLogger, true)
                .Build();

string? scope = "https://graph.microsoft.com";

do
{
    Console.WriteLine($"Acquiring token with scope {scope}");

    try
    {
        var result = await mi.AcquireTokenForManagedIdentity(scope)
            .WithMtlsProofOfPossession()
            .ExecuteAsync().ConfigureAwait(false);

        Console.WriteLine(result.AccessToken);
        Console.WriteLine(result.BindingCertificate);

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
