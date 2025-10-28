// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Identity.Client.MtlsPop;

internal class Program
{
    // App state
    private static IManagedIdentityApplication s_miApp = null!;
    private static ManagedIdentityId s_currentMiId = ManagedIdentityId.SystemAssigned;
    private static string s_identityLabel = "SAMI";

    // Defaults
    private static string s_resource = "https://graph.microsoft.com";
    private const string DefaultUamiClientId = "209b9435-3a7d-4967-8647-52c648d6f67f";
    private const string DefaultUamiObjectId = "981430e1-6890-498c-b882-7f7a0cf853fe";
    private const string DefaultUamiResourceId =
        "/subscriptions/ff71c235-108e-4869-9779-5f275ce45c44/resourcegroups/nbhargava/providers/Microsoft.ManagedIdentity/userAssignedIdentities/nidhi_uai_centraluseuap";

    // Session flags
    private static bool s_forceRefresh = false;        // persistent toggle
    private static bool s_forceRefreshNext = false;    // one-shot bypass
    private static bool s_lastWasBound = false;
    private static bool s_hasLast = false;

    // Toggle for full token printing (default OFF)
    private static bool s_printFullToken = false;

    private static async Task Main()
    {
        Console.Clear();
        WriteTitle("Managed Identity Token Tester");

        // Resource first (keeps intuitive default)
        s_resource = NormalizeResource(Ask($"Resource", defaultValue: s_resource));

        var logger = new IdentityLogger();
        BuildMiApp(ManagedIdentityId.SystemAssigned, "SAMI", logger);

        PrintHelp();

        while (true)
        {
            DrawStatusBar();
            WriteMenu();

            Console.Write("\nChoice (A/P/T/B/F/I/R/H/Enter/X): ");
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                if (!s_hasLast)
                {
                    Info("No previous acquisition yet. Press A or P first.");
                    continue;
                }
                await AcquireAndReportAsync(bound: s_lastWasBound, forceRefresh: s_forceRefresh).ConfigureAwait(false);
                continue;
            }

            var ch = char.ToUpperInvariant(key.KeyChar);
            Console.WriteLine(ch == '\0' ? "" : ch.ToString());

            try
            {
                switch (ch)
                {
                    case 'A': // Bearer
                        await AcquireAndReportAsync(bound: false, forceRefresh: s_forceRefresh).ConfigureAwait(false);
                        s_lastWasBound = false;
                        s_hasLast = true;
                        break;

                    case 'P': // PoP (mTLS-bound)
                        await AcquireAndReportAsync(bound: true, forceRefresh: s_forceRefresh).ConfigureAwait(false);
                        s_lastWasBound = true;
                        s_hasLast = true;
                        break;

                    case 'T': // Toggle persistent Force Refresh
                        s_forceRefresh = !s_forceRefresh;
                        Info($"Force Refresh is now {(s_forceRefresh ? "ON" : "OFF")}.");
                        break;

                    case 'B': // One-shot Force Refresh (next acquisition only)
                        s_forceRefreshNext = true;
                        Info("Next acquisition will bypass cache (one-shot).");
                        break;

                    case 'F': // Toggle full token printing
                        s_printFullToken = !s_printFullToken;
                        Info($"Full Token Print is now {(s_printFullToken ? "ON" : "OFF")}.");
                        if (s_printFullToken)
                        {
                            Error("WARNING: Full token printing exposes secrets. Do NOT use in shared terminals or logs.");
                        }
                        break;

                    case 'I': // Identity
                        await SwitchIdentityAsync(logger).ConfigureAwait(false);
                        break;

                    case 'R': // Resource
                        var newRes = Ask("New resource (blank=keep)", allowEmpty: true);
                        if (!string.IsNullOrWhiteSpace(newRes))
                        {
                            s_resource = NormalizeResource(newRes);
                            Success($"Resource set to {s_resource}");
                        }
                        break;

                    case 'H':
                        PrintHelp();
                        break;

                    case 'X':
                        Console.WriteLine("\nGoodbye.");
                        return;

                    default:
                        Info("Unknown choice. Press H for help.");
                        break;
                }
            }
            catch (MsalServiceException ex)
            {
                Error("[MSAL Service Error]");
                Console.WriteLine($"  Code   : {ex.ErrorCode}");
                Console.WriteLine($"  Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Error("[Unexpected Error]");
                Console.WriteLine(ex.ToString());
            }
        }
    }

    // === UX ===

    private static void WriteTitle(string text)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(text);
        Console.ForegroundColor = old;
        Console.WriteLine();
    }

    private static void DrawStatusBar()
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n================ STATUS ================");
        Console.WriteLine($"Resource : {s_resource}");
        Console.WriteLine($"Identity : {s_identityLabel}");
        Console.WriteLine($"Mode     : {(s_lastWasBound ? "Bound (mTLS PoP)" : "Bearer")}, ForceRefresh={(s_forceRefresh ? "ON" : "OFF")}, NextBypass={(s_forceRefreshNext ? "ON" : "OFF")}");
        Console.WriteLine($"Secrets  : FullTokenPrint={(s_printFullToken ? "ON" : "OFF")}");
        Console.WriteLine("=======================================\n");
        Console.ForegroundColor = old;
    }

    private static void WriteMenu()
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("A) Acquire Bearer token");
        Console.WriteLine("P) Acquire Bound token (mTLS PoP)");
        Console.WriteLine("T) Toggle Force Refresh (persistent)");
        Console.WriteLine("B) Bypass cache on NEXT acquisition (one-shot)");
        Console.WriteLine("F) Toggle Full Token Print");
        Console.WriteLine("I) Switch Identity (SAMI/UAMI presets)");
        Console.WriteLine("R) Change Resource");
        Console.WriteLine("H) Help");
        Console.WriteLine("Enter) Repeat last acquisition");
        Console.WriteLine("X) Exit");
        Console.ForegroundColor = old;
    }

    private static void PrintHelp()
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Help:");
        Console.WriteLine(" - A: Get a Bearer token for the current resource.");
        Console.WriteLine(" - P: Get a Bound (mTLS PoP) token (only useful if the API accepts cert-bound tokens).");
        Console.WriteLine(" - T: Toggle Force Refresh to bypass cache on every acquisition until turned off.");
        Console.WriteLine(" - B: One-shot bypass; only the NEXT acquisition will bypass cache, then it resets.");
        Console.WriteLine(" - F: Toggle Full Token Print. WARNING: prints the entire token (secret).");
        Console.WriteLine(" - I: Switch identity. Options:");
        Console.WriteLine("      1) SAMI");
        Console.WriteLine("      2) UAMI (ClientId)   [default: 209b9435-...-52c648d6f67f]");
        Console.WriteLine("      3) UAMI (ResourceId) [default: …/nidhi_uai_centraluseuap]");
        Console.WriteLine("      4) UAMI (ObjectId)   [default: 981430e1-...-7f7a0cf853fe]");
        Console.WriteLine(" - R: Change the resource (accepts `/.default`, will normalize).");
        Console.WriteLine(" - Enter: Repeat the last acquisition (great for cache testing).");
        Console.ForegroundColor = old;
        Console.WriteLine();
    }

    private static void Success(string msg)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(msg);
        Console.ForegroundColor = old;
    }
    private static void Info(string msg)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(msg);
        Console.ForegroundColor = old;
    }
    private static void Error(string msg)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ForegroundColor = old;
    }

    // === Identity + Acquire ===

    private static async Task SwitchIdentityAsync(IIdentityLogger logger)
    {
        Console.WriteLine();
        Console.WriteLine("Identities:");
        Console.WriteLine("  1) SAMI");
        Console.WriteLine("  2) UAMI (ClientId)");
        Console.WriteLine("  3) UAMI (ResourceId)");
        Console.WriteLine("  4) UAMI (ObjectId)");

        var pick = Ask("Selection", defaultValue: "1");
        switch (pick)
        {
            case "1":
                BuildMiApp(ManagedIdentityId.SystemAssigned, "SAMI", logger);
                break;

            case "2":
                {
                    var id = Ask("UAMI ClientId (GUID)", defaultValue: DefaultUamiClientId);
                    BuildMiApp(ManagedIdentityId.WithUserAssignedClientId(id), $"UAMI (ClientId={Short(id)})", logger);
                }
                break;

            case "3":
                {
                    var rid = Ask("UAMI ResourceId", defaultValue: DefaultUamiResourceId);
                    BuildMiApp(ManagedIdentityId.WithUserAssignedResourceId(rid), "UAMI (ResourceId=…/nidhi_uai_centraluseuap)", logger);
                }
                break;

            case "4":
                {
                    var oid = Ask("UAMI ObjectId (GUID)", defaultValue: DefaultUamiObjectId);
                    BuildMiApp(ManagedIdentityId.WithUserAssignedObjectId(oid), $"UAMI (ObjectId={Short(oid)})", logger);
                }
                break;

            default:
                Info("Unknown selection. Keeping current identity.");
                break;
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static void BuildMiApp(ManagedIdentityId miId, string label, IIdentityLogger logger)
    {
        s_currentMiId = miId;
        s_identityLabel = label;
        s_miApp = ManagedIdentityApplicationBuilder
            .Create(miId)
            .WithLogging(logger, enablePiiLogging: true)
            .Build();
        Success($"Identity set to {s_identityLabel}");
    }

    private static async Task AcquireAndReportAsync(bool bound, bool forceRefresh)
    {
        // Compute effective force refresh (global OR one-shot), then consume one-shot
        bool effectiveForceRefresh = forceRefresh || s_forceRefreshNext;
        s_forceRefreshNext = false; // consume one-shot if it was set

        Info($"\nAcquiring {(bound ? "BOUND (mTLS PoP)" : "BEARER")} token for {s_resource} ...");

        var builder = s_miApp.AcquireTokenForManagedIdentity(s_resource);
        if (bound)
            builder = builder.WithMtlsProofOfPossession();
        if (effectiveForceRefresh)
            builder = builder.WithForceRefresh(true);

        var result = await builder.ExecuteAsync().ConfigureAwait(false);

        Success("Success!");
        var source = result.AuthenticationResultMetadata?.TokenSource.ToString() ?? "Unknown";
        Console.WriteLine($"  Token source : {source}");
        Console.WriteLine($"  Expires On   : {result.ExpiresOn.UtcDateTime:O} (UTC)");
        Console.WriteLine($"  Token type   : {(bound ? "Bound (mTLS PoP)" : "Bearer")}");

        if (s_printFullToken)
        {
            Error("  NOTE: Printing full token (secret) per toggle!");
            Console.WriteLine($"  Access Token : {result.AccessToken}");
        }
        else
        {
            var preview = result.AccessToken?.Length > 32 ? result.AccessToken[..32] + "..." : result.AccessToken;
            Console.WriteLine($"  Access Token : {preview}");
        }

        if (bound && !string.IsNullOrEmpty(result.AccessToken))
        {
            var cnf = TryGetCnfClaim(result.AccessToken);
            if (cnf is not null)
            {
                Console.WriteLine("  PoP cnf      :");
                if (cnf.Value.TryGetProperty("x5t#S256", out var x5t))
                    Console.WriteLine($"    x5t#S256   : {x5t.GetString()}");
                if (cnf.Value.TryGetProperty("kid", out var kid))
                    Console.WriteLine($"    kid        : {kid.GetString()}");
                if (cnf.Value.TryGetProperty("xms_mirid", out var mirid))
                    Console.WriteLine($"    xms_mirid  : {mirid.GetString()}");
            }
            else
            {
                Console.WriteLine("  PoP cnf      : (not present)");
            }
        }
    }

    // === Helpers ===

    private static string Ask(string prompt, string? defaultValue = null, bool allowEmpty = false)
    {
        if (defaultValue is null)
            Console.Write($"{prompt}: ");
        else
            Console.Write($"{prompt} [{defaultValue}]: ");

        while (true)
        {
            var s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s))
            {
                if (defaultValue != null)
                    return defaultValue;
                if (allowEmpty)
                    return string.Empty;
            }
            else
            {
                return s.Trim();
            }
            Console.Write("Please enter a value: ");
        }
    }

    private static string NormalizeResource(string input)
    {
        var s = input.Trim();
        if (s.EndsWith("/.default", StringComparison.OrdinalIgnoreCase))
            s = s[..^"/.default".Length];
        if (s.EndsWith("/"))
            s = s.TrimEnd('/');
        return s;
    }

    private static string Short(string id) => id.Length <= 8 ? id : id[..8];

    private static JsonElement? TryGetCnfClaim(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return null;
        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var doc = JsonDocument.Parse(payloadJson);
        if (!doc.RootElement.TryGetProperty("cnf", out var cnfEl))
            return null;
        using var cnfDoc = JsonDocument.Parse(cnfEl.GetRawText());
        return cnfDoc.RootElement.Clone();
    }

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2:
                s += "==";
                break;
            case 3:
                s += "=";
                break;
        }
        return Convert.FromBase64String(s);
    }
}

class IdentityLogger : IIdentityLogger
{
    public EventLogLevel MinLogLevel { get; } = EventLogLevel.Verbose;
    public bool IsEnabled(EventLogLevel eventLogLevel) => eventLogLevel <= MinLogLevel;
    public void Log(LogEntry entry) => Console.WriteLine($"[MSAL] {entry.Message}");
}
