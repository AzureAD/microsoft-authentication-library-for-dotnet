// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main()
    {
        // ─── 0. Environment banner ────────────────────────────────────────────────
        Console.WriteLine(new string('═', 70));
        
        // ─── 1. Certificate events ────────────────────────────────────────────────
        ManagedIdentityApplication.BindingCertificateUpdated += c =>
        {
            Console.WriteLine($"\n[{Now()}] Event Fired -  BindingCertificateUpdated:");
            PrintCertificate(c);
        };

        // ─── 2. Managed-identity source & certs ───────────────────────────────────
        var source = await ManagedIdentityApplication.GetManagedIdentitySourceAsync()
                                                     .ConfigureAwait(false);
        Console.WriteLine($"\n[{Now()}] Managed Identity Source: {source}");

        var cert = ManagedIdentityApplication.GetManagedIdentityBindingCertificate();
        PrintCertificate(cert, "Initial Managed-Identity Binding Certificate");

        //sleep for 5 second so we can see different timestamps
        Thread.Sleep(5000);
        
        cert = ManagedIdentityApplication.ForceUpdateInMemoryCertificate();
        PrintCertificate(cert, "Forced NEW Managed-Identity Binding Certificate");

        // ─── 3. Build MI app ──────────────────────────────────────────────────────
        IIdentityLogger logger = new IdentityLogger();
        IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder
                                         .Create(ManagedIdentityId.SystemAssigned)
                                         .WithExperimentalFeatures()
                                         .WithLogging(logger, true)
                                         .Build();

        // ─── 4. Token-acquisition loop ───────────────────────────────────────────
        string scope = "https://vault.azure.net/";
        while (true)
        {
            Console.WriteLine($"\n[{Now()}] Acquiring token for scope → {scope}");
            try
            {
                var result = await mi.AcquireTokenForManagedIdentity(scope)
                                     //.WithProofOfPossession()
                                     .ExecuteAsync()
                                     .ConfigureAwait(false);

                Console.WriteLine($"[{Now()}] ✅  Success (expires {result.ExpiresOn:HH:mm:ss})");
                Console.WriteLine($"Access-Token (first 100 chars): {result.AccessToken[..100]}…");
            }
            catch (MsalServiceException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{Now()}] ❌  {ex.ErrorCode}: {ex.Message}");
                Console.ResetColor();
            }

            Console.Write("\nEnter new scope or 'q' to quit: ");
            var input = Console.ReadLine();
            if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
                break;
            if (!string.IsNullOrWhiteSpace(input))
                scope = input.Trim();
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static string Now() => DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");

    private static void PrintCertificate(X509Certificate2 cert, string? title = null)
    {
        Console.WriteLine("\n" + (title ?? "Certificate details") + "\n" + new string('-', 60));
        Console.WriteLine($"Subject     : {cert.Subject}");
        Console.WriteLine($"Issuer      : {cert.Issuer}");
        Console.WriteLine($"Serial #    : {cert.SerialNumber}");
        Console.WriteLine($"Not Before  : {cert.NotBefore:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Not After   : {cert.NotAfter:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Thumbprint  : {cert.Thumbprint}");
        Console.WriteLine(new string('-', 60));
    }
}

/// <summary>Minimal ILogger that pipes everything to Console.</summary>
class IdentityLogger : IIdentityLogger
{
    public EventLogLevel MinLogLevel => EventLogLevel.Verbose;
    public bool IsEnabled(EventLogLevel level) => level <= MinLogLevel;

    public void Log(LogEntry entry) => Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] {entry.Message}");
}
