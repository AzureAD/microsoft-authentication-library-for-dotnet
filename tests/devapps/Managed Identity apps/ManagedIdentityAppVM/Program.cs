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
using Microsoft.Identity.Client.KeyAttestation;
using System.Security.Principal;

internal class Program
{
    // App state
    private static IManagedIdentityApplication s_miApp = null!;
    private static ManagedIdentityId s_currentMiId = ManagedIdentityId.SystemAssigned;
    private static string s_identityLabel = "SAMI";
    private static IManagedIdentityApplication? s_rdpMiApp;

    // Defaults
    private static string s_resource = "https://graph.microsoft.com";
    private const string DefaultUamiClientId = "209b9435-3a7d-4967-8647-52c648d6f67f";
    private const string DefaultUamiObjectId = "981430e1-6890-498c-b882-7f7a0cf853fe";
    private const string DefaultUamiResourceId =
        "/subscriptions/ff71c235-108e-4869-9779-5f275ce45c44/resourcegroups/nbhargava/providers/Microsoft.ManagedIdentity/userAssignedIdentities/nidhi_uai_centraluseuap";

    // Session flags
    private static bool s_forceRefresh = false;        // persistent toggle
    private static bool s_forceRefreshNext = false;    // one-shot bypass
    private static bool s_hasLast = false;

    // Toggle for full token printing (default OFF)
    private static bool s_printFullToken = false;
    private static bool s_showImdsErrorDetails;

    private enum AcquireMode
    {
        Bearer,
        BoundWithAttestation,
        EndorsedUnattested
    }

    private static AcquireMode s_lastMode = AcquireMode.Bearer;

    private static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[args.Length - 1] == "--imds-error-details")
            {
                s_showImdsErrorDetails = true;
                Array.Resize(ref args, args.Length - 1);
                Console.WriteLine("IMDS error details enabled: service error text may contain identifiers. Review before sharing.");
            }
            if (args.Length == 1 && args[0] == "--help")
            {
                Console.WriteLine("No arguments: existing interactive MSI tester; N selects the endorsed unattested flow for token binding.");
                Console.WriteLine("--rdp-live <resource> [UAMI client ID]: one live acquisition through the modified MSAL project.");
                Console.WriteLine("Append --imds-error-details to display service error fields on failure.");
                Console.WriteLine("Uses a workload binding key, endorsed by RDP/xrdp when found; no endorsement-key permission changes.");
                return 0;
            }
            if (args.Length > 0)
            {
                if (args[0] != "--rdp-live" || args.Length is < 2 or > 3 ||
                    !Uri.TryCreate(args[1], UriKind.Absolute, out _) ||
                    (args.Length == 3 && (!Guid.TryParse(args[2], out Guid id) || id == Guid.Empty)))
                {
                    Error("Usage: --rdp-live <absolute-resource-uri> [UAMI client ID], or no arguments for the interactive tester.");
                    return 1;
                }
                s_resource = NormalizeResource(args[1]);
                s_currentMiId = args.Length == 3
                    ? ManagedIdentityId.WithUserAssignedClientId(args[2]) : ManagedIdentityId.SystemAssigned;
                await AcquireAndReportAsync(AcquireMode.EndorsedUnattested, forceRefresh: false).ConfigureAwait(false);
                return 0;
            }
            await RunInteractiveAsync().ConfigureAwait(false);
            return 0;
        }
        catch (MsalException ex)
        {
            ReportMsalError(ex);
            return 1;
        }
        catch (Exception ex)
        {
            Error($"Test app failed: {ex.GetType().Name}; HRESULT=0x{ex.HResult:X8}");
            return 1;
        }
        finally
        {
            ResetRdpApp();
        }
    }

    private static void ReportMsalError(MsalException exception)
    {
        Error($"MSAL error: {exception.ErrorCode}: {exception.Message}");
        if (exception is MsalServiceException serviceError && serviceError.StatusCode > 0)
        {
            Console.WriteLine($"HTTP status: {serviceError.StatusCode}");
            WriteImdsErrorDetails(serviceError);
        }
    }

    private static void WriteImdsErrorDetails(MsalServiceException exception)
    {
        if (string.IsNullOrWhiteSpace(exception.ResponseBody))
        {
            return;
        }
        if (!s_showImdsErrorDetails)
        {
            Console.WriteLine("Service error details are available. Append --imds-error-details to display them.");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(exception.ResponseBody);
            bool displayed = false;
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (string field in new[] { "error", "error_description", "message", "correlationId", "correlation_id", "trace_id", "timestamp" })
                {
                    if (document.RootElement.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String)
                    {
                        Console.WriteLine($"IMDS {field}: {JsonSerializer.Serialize(value.GetString())}");
                        displayed = true;
                    }
                }
            }
            if (!displayed)
            {
                Console.WriteLine("IMDS returned no recognized error fields; the response body was not displayed.");
            }
        }
        catch (JsonException)
        {
            Console.WriteLine("IMDS returned a non-JSON error body; the response body was not displayed.");
        }
    }

    private static async Task RunInteractiveAsync()
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

            Console.Write("\nChoice (A/P/N/T/B/F/I/R/H/Enter/X): ");
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                if (!s_hasLast)
                {
                    Info("No previous acquisition yet. Press A, P or N first.");
                    continue;
                }

                await AcquireAndReportAsync(mode: s_lastMode, forceRefresh: s_forceRefresh).ConfigureAwait(false);
                continue;
            }

            var ch = char.ToUpperInvariant(key.KeyChar);
            Console.WriteLine(ch == '\0' ? "" : ch.ToString());

            try
            {
                switch (ch)
                {
                    case 'A': // Bearer
                        await AcquireAndReportAsync(mode: AcquireMode.Bearer, forceRefresh: s_forceRefresh).ConfigureAwait(false);
                        s_lastMode = AcquireMode.Bearer;
                        s_hasLast = true;
                        break;

                    case 'P': // PoP (mTLS-bound) WITH attestation
                        await AcquireAndReportAsync(mode: AcquireMode.BoundWithAttestation, forceRefresh: s_forceRefresh).ConfigureAwait(false);
                        s_lastMode = AcquireMode.BoundWithAttestation;
                        s_hasLast = true;
                        break;

                    case 'N': // PoP (mTLS-bound) WITHOUT attestation
                        await AcquireAndReportAsync(mode: AcquireMode.EndorsedUnattested, forceRefresh: s_forceRefresh).ConfigureAwait(false);
                        s_lastMode = AcquireMode.EndorsedUnattested;
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
            catch (MsalException ex)
            {
                ReportMsalError(ex);
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

        string modeLabel = s_lastMode switch
        {
            AcquireMode.Bearer => "Bearer",
            AcquireMode.BoundWithAttestation => "Bound (mTLS PoP + Attestation)",
            AcquireMode.EndorsedUnattested => "Bound (workload key, optional RDP endorsement)",
            _ => "Unknown"
        };

        Console.WriteLine($"Mode     : {modeLabel}, ForceRefresh={(s_forceRefresh ? "ON" : "OFF")}, NextBypass={(s_forceRefreshNext ? "ON" : "OFF")}");
        Console.WriteLine($"Secrets  : FullTokenPrint={(s_printFullToken ? "ON" : "OFF")}");
        Console.WriteLine("=======================================\n");
        Console.ForegroundColor = old;
    }

    private static void WriteMenu()
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("A) Acquire Bearer token");
        Console.WriteLine("P) Acquire Bound token (mTLS PoP) WITH attestation");
        Console.WriteLine("N) Acquire Bound token using a workload key with optional RDP endorsement");
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
        Console.WriteLine(" - P: Get a Bound (mTLS PoP) token WITH attestation support.");
        Console.WriteLine(" - N: Bind to a workload key; use RDP/xrdp endorsement when found (SAMI or UAMI ClientId).");
        Console.WriteLine("      Acquisition only; no resource TLS. The current identity must have private-key access.");
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
        ResetRdpApp();
        Success($"Identity set to {s_identityLabel}");
    }

    private static void ResetRdpApp()
    {
        s_rdpMiApp = null;
    }

    private static IManagedIdentityApplication GetRdpApp()
    {
        if (s_rdpMiApp is not null)
        {
            return s_rdpMiApp;
        }
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            Info("Execution identity: " + identity.Name);
        }
        else
        {
            Info("Execution identity: " + Environment.UserName);
        }
        s_rdpMiApp = ManagedIdentityApplicationBuilder.Create(s_currentMiId)
            .WithLogging(new IdentityLogger(), enablePiiLogging: false)
            .Build();
        return s_rdpMiApp;
    }

    private static async Task AcquireAndReportAsync(AcquireMode mode, bool forceRefresh)
    {
        // Compute effective force refresh (global OR one-shot), then consume one-shot
        bool effectiveForceRefresh = forceRefresh || s_forceRefreshNext;
        s_forceRefreshNext = false; // consume one-shot if it was set

        Info($"\nAcquiring {mode} token for {s_resource} ...");

        bool useRdp = mode == AcquireMode.EndorsedUnattested;
        var app = useRdp ? GetRdpApp() : s_miApp;
        var builder = app.AcquireTokenForManagedIdentity(s_resource);

        bool bound = useRdp || mode == AcquireMode.BoundWithAttestation;
        bool useAttestation = mode == AcquireMode.BoundWithAttestation;

        if (bound)
        {
            builder = builder.WithMtlsProofOfPossession();
            if (useAttestation)
            {
                builder = builder.WithAttestationSupport();
            }
        }

        if (effectiveForceRefresh)
            builder = builder.WithForceRefresh(true);

        var result = await builder.ExecuteAsync().ConfigureAwait(false);

        Success("Success!");
        var source = result.AuthenticationResultMetadata?.TokenSource.ToString() ?? "Unknown";
        Console.WriteLine($"  Token source : {source}");
        Console.WriteLine($"  Expires On   : {result.ExpiresOn.UtcDateTime:O} (UTC)");
        Console.WriteLine($"  Token type   : {result.TokenType}");

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

        if (useRdp)
        {
            Console.WriteLine($"  Certificate  : {result.BindingCertificate?.Thumbprint ?? "(not returned)"}");
        }
        if (bound && !useRdp && !string.IsNullOrEmpty(result.AccessToken))
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
