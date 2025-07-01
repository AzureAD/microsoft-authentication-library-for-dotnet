// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;

// Binding certificate event handler
ManagedIdentityApplication.BindingCertificateUpdated += cert =>
{
    Console.WriteLine($"BindingCertificateUpdated - Binding cert thumbprint: {cert.Thumbprint}");
};

//Get Managed Identity Source
Console.WriteLine("Managed Identity Source is {0}", 
    await ManagedIdentityApplication.GetManagedIdentitySourceAsync()
    .ConfigureAwait(false));

// Get Managed Identity Binding Certificate
var cert = ManagedIdentityApplication.GetManagedIdentityBindingCertificate();
Console.WriteLine($"Managed Identity Binding Certificate: {cert.GetExpirationDateString}");
Console.WriteLine("Get Managed Identity Binding Certificate : {0}", cert);

// Force updated Managed Identity Binding Certificate
cert = ManagedIdentityApplication.ForceUpdateInMemoryCertificate();
Console.WriteLine($"Managed Identity Binding Certificate: {cert.GetExpirationDateString}");
Console.WriteLine("Get New Managed Identity Binding Certificate : {0}", cert);

IIdentityLogger identityLogger = new IdentityLogger();

IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .WithExperimentalFeatures()
                .WithLogging(identityLogger, true)
                .Build();

string? scope = "https://vault.azure.net/";

do
{
    Console.WriteLine($"Acquiring token with scope {scope}");

    try
    {
        var result = await mi.AcquireTokenForManagedIdentity(scope)
            //.WithProofOfPossession()
            .ExecuteAsync().ConfigureAwait(false);

        Console.WriteLine(result.AccessToken);
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
