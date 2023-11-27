// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using OpenTelemetry;
using OpenTelemetry.Metrics;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        //// Add a console exporter for metrics to display on the console.
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("MicrosoftIdentityClient_Common_Meter")
            .AddConsoleExporter()
            .Build();

        // Successful requests
        IConfidentialAppSettings s_appSettings = ConfidentialAppSettings.GetSettings(Cloud.Public);
        string[] scopes = new string[] { $"{s_appSettings.ClientId}/.default", };
        var builder = ConfidentialClientApplicationBuilder.Create(s_appSettings.ClientId)
            .WithAuthority(s_appSettings.Authority, false)
            .WithCertificate(s_appSettings.GetCertificate())
            .WithLogging(Log, LogLevel.Verbose, true);

        var cca = builder.Build();

        AuthenticationResult result;

        for(int i = 0; i < 10; i++)
        {
            result = await cca.AcquireTokenForClient(scopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            _ = Task.Delay(100);
        }

        // Failed requests
        builder = ConfidentialClientApplicationBuilder.Create(s_appSettings.ClientId)
            .WithAuthority(s_appSettings.Authority, false)
            .WithClientSecret("invalid")
            .WithLogging(Log, LogLevel.Verbose, true);

        cca = builder.Build();

        for (int i = 0; i < 2; i++)
        {
            try
            {
                result = await cca.AcquireTokenForClient(scopes)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        //Add delay to let the exporter collect metrics and activity.
        await Task.Delay(60000).ConfigureAwait(false);

    }

    private static void Log(LogLevel level, string message, bool containsPii)
    {
        if (containsPii)
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
        Console.WriteLine($"{level} {message}");
        Console.ResetColor();
    }
}
