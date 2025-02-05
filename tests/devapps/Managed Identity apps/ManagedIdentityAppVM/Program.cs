﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;

// Subscribe to certificate rotation notifications.
ManagedIdentityApplication.BindingCertificateRotated += cert =>
{
    Console.WriteLine($"[Event] Binding certificate rotated. New certificate thumbprint: {cert.Thumbprint}");
};

IIdentityLogger identityLogger = new IdentityLogger();

IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId("5bcd1685-b002-4fd1-8ebd-1ec3e1e4ca4d"))
                .WithLogging(identityLogger, true)
                .Build();

string? scope = "https://management.azure.com";

do
{
    Console.WriteLine($"Acquiring token with scope {scope}");

    try
    {
        ManagedIdentityApplication.GetBindingCertificate(); 

        var result = await mi.AcquireTokenForManagedIdentity(scope)
            .ExecuteAsync().ConfigureAwait(false);

        ManagedIdentityApplication.GetBindingCertificate();

        result = await mi.AcquireTokenForManagedIdentity(scope)
            .ExecuteAsync().ConfigureAwait(false);

        ManagedIdentityApplication.GetBindingCertificate();

        result = await mi.AcquireTokenForManagedIdentity(scope).WithForceRefresh(true)
            .ExecuteAsync().ConfigureAwait(false);

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
