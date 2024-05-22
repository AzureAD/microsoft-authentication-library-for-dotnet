// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;

IIdentityLogger identityLogger = new IdentityLogger();

string claims = @"{""access_token"":{""nbf"":{""essential"":true, ""value"":""1701477303""}}}";

Console.WriteLine($"Binding Certificate - { ManagedIdentityApplication.GetBindingCertificate() }");

Console.WriteLine($"Claims supported in MI ?  - { ManagedIdentityApplication.IsClaimsSupportedByClient() }");

IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder
    //.Create(ManagedIdentityId.SystemAssigned)
    //.Create(ManagedIdentityId.WithUserAssignedClientId("8a7c2bc8-7041-4eb9-b49a-a70aeb68fdae")) // CAE VM 
    .Create(ManagedIdentityId.WithUserAssignedClientId("3b57c42c-3201-4295-ae27-d6baec5b7027")) //MSAL SLC VM
    .WithExperimentalFeatures(true)
                .WithClientCapabilities(new string[] { "CP1" })
                .WithLogging(identityLogger, true)
                .Build();

string? scope = "https://management.azure.com";

string? resource = "api://AzureAdTokenExchange";
do
{
    Console.WriteLine($"Acquiring token with scope {scope}");

    try
    {
        var result = await mi.AcquireTokenForManagedIdentity(resource)
            .WithClaims(claims)
            .ExecuteAsync()
            .ConfigureAwait(false);

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
