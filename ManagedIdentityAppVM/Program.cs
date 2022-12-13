// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create("00bedee1-0e09-4a8d-81a0-0679c5a64a83")
                .WithExperimentalFeatures()
                .WithDebugLoggingCallback(logLevel: LogLevel.Verbose, enablePiiLogging: true, withDefaultPlatformLoggingEnabled: true)
                .Build();

string scope = "https://management.azure.com";

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

    Console.WriteLine("Enter the scope to acquire token, 'q' to quit.");
    scope = Console.ReadLine();
} while (!scope.Equals("q", StringComparison.InvariantCultureIgnoreCase));
