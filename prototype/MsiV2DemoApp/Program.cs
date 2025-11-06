// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Managed Identity + mTLS PoP + mTLS resource call (Console Demo)
// .NET 8 
//
// Menu:
//   Acquire Tokens
//     1   - SAMI → Token from IDP (force refresh)
//     1a  - SAMI → Token from Cache
//     2   - UAMI → Token from IDP (force refresh)
//     2a  - UAMI → Token from Cache
//   Call Resource
//     3   - SAMI token + cert → Call resource (mTLS)
//     4   - UAMI token + cert → Call resource (mTLS)
//   Display & Toggles
//     F   - Toggle Full Token view (ON/OFF)
//     L   - Toggle MSAL logging (ON/OFF; off by default)
//   Settings
//     set-uami     - Change UAMI client id
//     set-resource - Change resource URL (default: https://mtlstb.graph.microsoft.com/v1.0/applications)
//     set-prop     - Change single property to display from Graph 'value[]' (default: displayName)
//   System
//     C / cls / clear - Clear screen
//     M   - Maximize window (Windows best-effort)
//     Q   - Quit
//
// Features:
// - mTLS PoP end-to-end with “Bound” check (token cnf.x5t#S256 vs cert SHA-256).
// - Animated spinners with Unicode/ASCII fallback.
// - Single-property output from Graph Applications (default "displayName"); change with set-prop.
// - MSAL logging available but OFF by default (toggle with L).
//
// DEV toggles (optional):
// - ACCEPT_ANY_SERVER_CERT=1 → accept any server TLS cert (DEV/LAB ONLY)
// - MSI_MTLS_TEST_CERT_THUMBPRINT or MSI_MTLS_TEST_CERT_SUBJECT [+ MSI_MTLS_TEST_CERT_STORE_LOC, MSI_MTLS_TEST_CERT_STORE_NAME]
//   → override client cert from Windows cert store
//
// NuGet: Microsoft.Identity.Client (Internal -Preview. Refer to the .md file)

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.MtlsPop;
using Microsoft.IdentityModel.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace DemoConsole;

internal class Program
{
    // ---------- Constants & P/Invoke (declare BEFORE use) ----------
    private const string DefaultScope = "https://graph.microsoft.com";
    private const string DefaultResourceUrl = "https://mtlstb.graph.microsoft.com/v1.0/applications";
    private const int SW_MAXIMIZE = 3;
    // Warn once per run when full-token view is enabled
    private static bool sFullTokenWarned = false;

    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // ---------- Entry Point ----------
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.InputEncoding = Encoding.UTF8;
        Console.Title = "Managed Identity + mTLS PoP Demo";
        TryMaximizeConsoleWindow(silent: true); // best-effort on Windows

        // Config with sensible defaults; UAMI/resource/property can be changed in-menu
        string uamiClientId = Environment.GetEnvironmentVariable("UAMI_CLIENT_ID")
            ?? "209b9435-3a7d-4967-8647-52c648d6f67f";
        string resourceUrl = Environment.GetEnvironmentVariable("MSI_MTLS_RESOURCE_URL") ?? DefaultResourceUrl;
        string propertyName = Environment.GetEnvironmentVariable("MSI_DEMO_PROPERTY") ?? "displayName";

        bool showLogs = args.Any(a => a.Equals("--log", StringComparison.OrdinalIgnoreCase) || a.Equals("-v", StringComparison.OrdinalIgnoreCase));
        bool showFullToken = false; // toggled live via 'F'

        var logger = new ToggleableLogger { Enabled = showLogs, Level = EventLogLevel.Informational };

        // Build MI apps once (cache stability)
        var sami = BuildMiApp(ManagedIdentityId.SystemAssigned, logger);
        var uami = BuildMiApp(ManagedIdentityId.WithUserAssignedClientId(uamiClientId), logger);

        AuthenticationResult? lastSami = null;
        AuthenticationResult? lastUami = null;

        WriteBanner();
        WriteAiHello();

        while (true)
        {
            PrintMenu(uamiClientId, resourceUrl, propertyName, showLogs, showFullToken);

            Console.Write("> ");
            var choice = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

            try
            {
                switch (choice)
                {
                    // Acquire tokens
                    case "1":
                        lastSami = await AcquireAndShowAsync("SAMI", sami, DefaultScope, forceRefresh: true, useMtls: true, showFullToken).ConfigureAwait(false);
                        break;

                    case "1a":
                        lastSami = await AcquireAndShowAsync("SAMI", sami, DefaultScope, forceRefresh: false, useMtls: true, showFullToken).ConfigureAwait(false);
                        break;

                    case "2":
                        lastUami = await AcquireAndShowAsync("UAMI", uami, DefaultScope, forceRefresh: true, useMtls: true, showFullToken).ConfigureAwait(false);
                        break;

                    case "2a":
                        lastUami = await AcquireAndShowAsync("UAMI", uami, DefaultScope, forceRefresh: false, useMtls: true, showFullToken).ConfigureAwait(false);
                        break;

                    // Call resource (mTLS) - SAMI
                    case "3":
                        lastSami = await CallResourceFlowAsync("SAMI", sami, lastSami, resourceUrl, propertyName, showFullToken).ConfigureAwait(false);
                        break;

                    // Call resource (mTLS) - UAMI
                    case "4":
                        lastUami = await CallResourceFlowAsync("UAMI", uami, lastUami, resourceUrl, propertyName, showFullToken).ConfigureAwait(false);
                        break;

                    // Display & Toggles
                    case "f":
                        showFullToken = !showFullToken;
                        PrintInfo($"Full token display is now {(showFullToken ? "ON" : "OFF")}.");
                        if (showFullToken && !sFullTokenWarned)
                        {
                            ShowFullTokenWarning();
                            sFullTokenWarned = true;
                        }
                        break;

                    case "l":
                        showLogs = !showLogs;
                        logger.Enabled = showLogs;
                        PrintInfo($"MSAL logging is now {(showLogs ? "ON" : "OFF")}.");
                        break;

                    // Settings
                    case "set-uami":
                        Console.Write("Enter new UAMI client id: ");
                        {
                            var newId = (Console.ReadLine() ?? "").Trim();
                            if (!string.IsNullOrEmpty(newId))
                            {
                                uamiClientId = newId;
                                uami = BuildMiApp(ManagedIdentityId.WithUserAssignedClientId(uamiClientId), logger);
                                lastUami = null;
                                PrintInfo($"UAMI client id set to {uamiClientId}");
                            }
                        }
                        break;

                    case "set-resource":
                        Console.Write("Enter new resource URL: ");
                        {
                            var newUrl = (Console.ReadLine() ?? "").Trim();
                            if (!string.IsNullOrEmpty(newUrl))
                            {
                                resourceUrl = newUrl;
                                PrintInfo($"Resource URL set to {resourceUrl}");
                            }
                        }
                        break;

                    case "set-prop":
                        Console.Write("Enter property to show from 'value[]' (e.g., displayName, appId, id): ");
                        {
                            var newProp = (Console.ReadLine() ?? "").Trim();
                            if (!string.IsNullOrEmpty(newProp))
                            {
                                propertyName = newProp;
                                PrintInfo($"Property set to {propertyName}");
                            }
                        }
                        break;

                    // System
                    case "c":
                    case "cls":
                    case "clear":
                        Console.Clear();
                        WriteBanner();
                        WriteAiHello();
                        break;

                    case "m":
                        TryMaximizeConsoleWindow(silent: false);
                        break;

                    case "q":
                    case "exit":
                        PrintFooter();
                        return;

                    case "h":
                    case "help":
                    case "?":
                        // just reprint the menu next loop
                        break;

                    default:
                        PrintWarn("Unknown choice. Type 'help' to see options.");
                        break;
                }
            }
            catch (MsalServiceException mse)
            {
                PrintError("MSAL service error", mse.Message);
            }
            catch (Exception ex)
            {
                PrintError("Unexpected error", ex.Message);
            }

            Console.WriteLine();
        }
    }

    // ---------- MSAL / MI ----------
    private static IManagedIdentityApplication BuildMiApp(ManagedIdentityId miId, IIdentityLogger logger) =>
        ManagedIdentityApplicationBuilder
            .Create(miId)
            .WithLogging(logger, enablePiiLogging: false)
            .Build();

    /// <summary>
    /// acquire token and show summary.
    /// </summary>
    /// <param name="label"></param>
    /// <param name="app"></param>
    /// <param name="scope"></param>
    /// <param name="forceRefresh"></param>
    /// <param name="useMtls"></param>
    /// <param name="showFullToken"></param>
    /// <returns></returns>
    private static async Task<AuthenticationResult?> AcquireAndShowAsync(
        string label,
        IManagedIdentityApplication app,
        string scope,
        bool forceRefresh,
        bool useMtls,
        bool showFullToken)
    {
        var builder = app.AcquireTokenForManagedIdentity(scope);
        if (useMtls) builder = builder.WithMtlsProofOfPossession();
        if (forceRefresh) builder = builder.WithForceRefresh(true);

        var result = await Ui.WithSpinnerAsync(
            $"{label}: acquiring token {(forceRefresh ? "(IDP)" : "(cache)")}{(useMtls ? " [mTLS]" : "")}",
            () => builder.ExecuteAsync()).ConfigureAwait(false);

        PrintTokenSummary(label, result, useMtls, forceRefresh, showFullToken);
        return result;
    }

    /// <summary>
    /// print token summary info.
    /// </summary>
    /// <param name="label"></param>
    /// <param name="result"></param>
    /// <param name="mtlsRequested"></param>
    /// <param name="forced"></param>
    /// <param name="showFullToken"></param>
    private static void PrintTokenSummary(string label, AuthenticationResult result, bool mtlsRequested, bool forced, bool showFullToken)
    {
        var ts = result.AuthenticationResultMetadata?.TokenSource.ToString() ?? "Unknown";
        var tokenType = result.TokenType ?? "access_token";
        var expUtc = result.ExpiresOn.UtcDateTime;
        var left = expUtc - DateTime.UtcNow; if (left < TimeSpan.Zero) left = TimeSpan.Zero;

        var aud = TryReadClaim(result.AccessToken, "aud");
        var tid = TryReadClaim(result.AccessToken, "tid");
        var appid = TryReadClaim(result.AccessToken, "appid");
        var oid = TryReadClaim(result.AccessToken, "oid");

        // Verify mTLS PoP binding: token cnf.x5t#S256 equals SHA-256 of cert
        var cnf = TryReadCnfX5tS256(result.AccessToken);
        bool bound = string.Equals(tokenType, "mtls_pop", StringComparison.OrdinalIgnoreCase)
                     && result.BindingCertificate is X509Certificate2 bc
                     && cnf is not null
                     && string.Equals(cnf, ComputeX5tS256(bc), StringComparison.Ordinal);

        Boxed($"[{label}] Token {(forced ? "from IDP (force refresh)" : "from Cache if valid")}");

        Console.WriteLine($"  Token Source   : {ts}");
        Console.WriteLine($"  Token Type     : {tokenType}");
        Console.WriteLine($"  Audience (aud) : {aud}");
        Console.WriteLine($"  Tenant  (tid)  : {tid}");
        Console.WriteLine($"  AppId   (appid): {appid}");
        Console.WriteLine($"  ObjectId(oid)  : {oid}");
        Console.WriteLine($"  Expires        : {expUtc:yyyy-MM-dd HH:mm:ss}Z  (in {left:hh\\:mm\\:ss})");

        if (bound)
            Console.WriteLine($"  mTLS PoP       : Bound {Glyphs.Check}");
        else if (mtlsRequested)
            Console.WriteLine($"  mTLS PoP       : Requested (not bound yet)");
        else
            Console.WriteLine($"  mTLS PoP       : No");

        if (!bound && mtlsRequested && result.BindingCertificate is not null && cnf is not null)
        {
            Console.WriteLine($"                   token x5t#S256={cnf}");
            Console.WriteLine($"                   cert  x5t#S256={ComputeX5tS256(result.BindingCertificate!)}");
        }

        if (showFullToken)
        {
            Console.WriteLine();
            Console.WriteLine("  Access Token   :");
            WithColor(ConsoleColor.Yellow, () => Console.WriteLine("    [Sensitive] Handle with care. Do not share/copy."));
            Console.WriteLine($"    {result.AccessToken}");
        }
        else
        {
            Console.WriteLine($"  Access Token   : {Abbrev(result.AccessToken)}   (press F to show full)");
        }

        if (result.BindingCertificate is X509Certificate2 cert)
        {
            Console.WriteLine();
            Console.WriteLine("  Binding Certificate:");
            Console.WriteLine($"    Subject    : {cert.Subject}");
            Console.WriteLine($"    Thumbprint : {cert.Thumbprint}");
            Console.WriteLine($"    NotBefore  : {cert.NotBefore.ToUniversalTime():yyyy-MM-dd HH:mm:ss}Z");
            Console.WriteLine($"    NotAfter   : {cert.NotAfter.ToUniversalTime():yyyy-MM-dd HH:mm:ss}Z");
            Console.WriteLine("    Note       : Presented in the client TLS handshake.");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("  Binding Certificate: (none)");
        }

        Console.WriteLine();
        Console.WriteLine($"  {Glyphs.Check} Token acquisition complete.");
    }

    /// <summary>
    /// call resource flow: ensure token+cert, then call resource over mTLS.
    /// </summary>
    /// <param name="label"></param>
    /// <param name="app"></param>
    /// <param name="lastResult"></param>
    /// <param name="resourceUrl"></param>
    /// <param name="propertyName"></param>
    /// <param name="showFullToken"></param>
    /// <returns></returns>
    private static async Task<AuthenticationResult?> CallResourceFlowAsync(
        string label,
        IManagedIdentityApplication app,
        AuthenticationResult? lastResult,
        string resourceUrl,
        string propertyName,
        bool showFullToken)
    {
        // Ensure token+cert (use cache if valid, else fetch)
        if (lastResult == null || lastResult.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(2))
        {
            lastResult = await AcquireAndShowAsync(label, app, DefaultScope, forceRefresh: false, useMtls: true, showFullToken).ConfigureAwait(false);
            if (lastResult == null) return lastResult;
        }

        var effectiveCert = TryLoadStoreCertOverride() ?? lastResult.BindingCertificate;
        if (effectiveCert == null)
        {
            PrintError("No binding certificate available", "mTLS call requires the binding certificate.");
            return lastResult;
        }

        await CallResourceWithMtlsAsync(new Uri(resourceUrl), effectiveCert, lastResult.TokenType, lastResult.AccessToken, propertyName).ConfigureAwait(false);
        return lastResult;
    }

    /// <summary>
    /// call resource over mTLS with given cert + token.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="clientCertificate"></param>
    /// <param name="tokenType"></param>
    /// <param name="accessToken"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    private static async Task CallResourceWithMtlsAsync(
    Uri url,
    X509Certificate2 clientCertificate,
    string tokenType,
    string accessToken,
    string propertyName)
    {
        using var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };
        handler.ClientCertificates.Add(clientCertificate);

        // DEV ONLY: accept any server cert (lab scenarios)
        if (Environment.GetEnvironmentVariable("ACCEPT_ANY_SERVER_CERT") == "1")
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            PrintWarn("Accepting any server certificate (DEV ONLY).");
        }

        using var http = new HttpClient(handler, disposeHandler: true);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, accessToken);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Header
        Boxed("Calling mTLS resource");
        Console.WriteLine($"  URL           : {url}");
        Console.WriteLine($"  Client Cert   : {clientCertificate.Subject} (Thumbprint={clientCertificate.Thumbprint})");
        Console.WriteLine($"  TLS           : {handler.SslProtocols}");

        // Mini phase readout (jigna!)
        WithColor(ConsoleColor.DarkGray, () =>
        {
            Console.WriteLine($"  Phase 1       : Present client certificate {Glyphs.Check}");
            Console.WriteLine($"  Phase 2       : Attach {tokenType} access token {Glyphs.Check}");
            Console.WriteLine($"  Phase 3       : Send GET to {url.Host} over mTLS …");
        });

        // Execute with spinner + timing
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var response = await Ui.WithSpinnerAsync(
            $"Calling {url.Host} over mTLS",
            () => http.GetAsync(url)).ConfigureAwait(false);
        sw.Stop();

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var ok = response.IsSuccessStatusCode;

        WithColor(ok ? ConsoleColor.Green : ConsoleColor.Red, () =>
        {
            Console.WriteLine($"  Status        : {(int)response.StatusCode} {response.StatusCode}  •  HTTP/{response.Version}  •  {sw.ElapsedMilliseconds} ms");
        });

        if (!ok)
        {
            Console.WriteLine("  Response Body :");
            Console.WriteLine(Indent(content, "    "));
            Console.WriteLine();
            Console.WriteLine($"  {Glyphs.Cross} mTLS resource call failed.");
            return;
        }

        // Extract single property from JSON array: value[]
        var items = new List<string>();
        int sizeBytes = Encoding.UTF8.GetByteCount(content);
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty(propertyName, out var val))
                    {
                        items.Add(val.ToString());
                    }
                }
            }
        }
        catch
        {
            Console.WriteLine("  Response Body (non-JSON or parse error):");
            Console.WriteLine(Indent(content, "    "));
            Console.WriteLine();
            Console.WriteLine($"  {Glyphs.Check} mTLS resource call complete.");
            return;
        }

        // Fancy success panel (auto-sizes to window width, ASCII fallback if needed)
        DrawSuccessPanel(
            total: items.Count,
            ms: sw.ElapsedMilliseconds,
            ver: response.Version,
            bytes: sizeBytes,
            host: url.Host);

        Console.WriteLine();
        WithColor(ConsoleColor.Cyan, () =>
            Console.WriteLine($"  Showing single property from result: {propertyName}"));
        Console.WriteLine($"  Count: {items.Count}");

        int max = Math.Min(items.Count, 12);
        for (int i = 0; i < max; i++)
        {
            Console.WriteLine($"    {Glyphs.Bullet} {items[i]}");
        }
        if (items.Count > max)
        {
            Console.WriteLine($"    ... (+{items.Count - max} more)");
        }

        Console.WriteLine();
        Console.WriteLine($"  {Glyphs.Check} mTLS resource call complete.");

        // ---------- local helpers (scoped to this method) ----------

        static void DrawSuccessPanel(int total, long ms, Version ver, int bytes, string host)
        {
            var bc = GetBoxChars();
            int width = GetPanelWidth(); // dynamic width for current console
            string horiz = new string(bc.H, Math.Max(4, width - 4));

            string line1 = $"  {bc.V}  mTLS call SUCCESS   {Glyphs.Bullet}   Items: {total,-5}   {Glyphs.Bullet}   {ms,5} ms   {Glyphs.Bullet}   HTTP/{ver}  ";
            string line2 = $"  {bc.V}  Host: {host,-40}  Size: {bytes,10:n0} bytes  ";

            WithColor(ConsoleColor.Green, () =>
            {
                Console.WriteLine();
                Console.WriteLine($"  {bc.TL}{horiz}{bc.TR}");
                PanelLine(line1);
                PanelLine(line2);
                Console.WriteLine($"  {bc.BL}{horiz}{bc.BR}");
            });

            void PanelLine(string s)
            {
                int inner = Math.Max(0, width - 4); // space between left/right borders
                string payload = s.Length - 3 > inner ? s[..(inner - 1)] + "…" : s;
                Console.WriteLine(payload.PadRight(inner + 3) + bc.V);
            }

            static int GetPanelWidth()
            {
                try
                {
                    // Leave a small margin; clamp between 60 and 120 for aesthetics
                    int w = Math.Clamp(Console.WindowWidth - 4, 60, 120);
                    return w;
                }
                catch { return 80; } // safe default
            }

            static (char TL, char TR, char BL, char BR, char H, char V) GetBoxChars()
            {
                if (Glyphs.Unicode)
                    return ('╔', '╗', '╚', '╝', '═', '║');
                else
                    return ('+', '+', '+', '+', '-', '|');
            }
        }
    }

    // ---------- Token/JWT helpers ----------
    private static string Abbrev(string token)
    {
        if (string.IsNullOrEmpty(token)) return "(empty)";
        if (token.Length <= 80) return token;
        return token[..60] + "..." + token[^20..];
    }

    /// <summary>
    /// read a claim from JWT token (null if not present).
    /// </summary>
    /// <param name="jwt"></param>
    /// <param name="claim"></param>
    /// <returns></returns>
    private static string? TryReadClaim(string jwt, string claim)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/')));
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(claim, out var val) ? val.ToString() : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// read cnf.x5t#S256 from JWT token (null if not present).
    /// </summary>
    /// <param name="jwt"></param>
    /// <returns></returns>
    private static string? TryReadCnfX5tS256(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/')));
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("cnf", out var cnf)
                && cnf.TryGetProperty("x5t#S256", out var x5t))
            {
                return x5t.GetString();
            }
            return null;
        }
        catch { return null; }
    }

    /// <summary>
    /// compute cert SHA-256 and return Base64Url-encoded string for x5t#S256.
    /// </summary>
    /// <param name="cert"></param>
    /// <returns></returns>
    private static string ComputeX5tS256(X509Certificate2 cert)
    {
        var hash = SHA256.HashData(cert.RawData);
        return ToBase64Url(hash);
    }

    /// <summary>
    /// to Base64Url encoding (no padding, - and _).
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private static string ToBase64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// optionally load cert override from Windows cert store (DEV only).
    /// </summary>
    /// <returns></returns>
    private static X509Certificate2? TryLoadStoreCertOverride()
    {
        var thumb = Environment.GetEnvironmentVariable("MSI_MTLS_TEST_CERT_THUMBPRINT");
        var subject = Environment.GetEnvironmentVariable("MSI_MTLS_TEST_CERT_SUBJECT");

        if (string.IsNullOrWhiteSpace(thumb) && string.IsNullOrWhiteSpace(subject))
            return null; // no override requested

        var locStr = Environment.GetEnvironmentVariable("MSI_MTLS_TEST_CERT_STORE_LOC") ?? "LocalMachine";
        var nameStr = Environment.GetEnvironmentVariable("MSI_MTLS_TEST_CERT_STORE_NAME") ?? "My";

        if (!Enum.TryParse(locStr, out StoreLocation location)) location = StoreLocation.LocalMachine;
        if (!Enum.TryParse(nameStr, out StoreName storeName)) storeName = StoreName.My;

        try
        {
            using var store = new X509Store(storeName, location);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            var certs = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

            X509Certificate2Collection? found = null;
            if (!string.IsNullOrWhiteSpace(thumb))
                found = certs.Find(X509FindType.FindByThumbprint, thumb, false);
            else if (!string.IsNullOrWhiteSpace(subject))
                found = certs.Find(X509FindType.FindBySubjectDistinguishedName, subject, false);

            if (found == null || found.Count == 0)
            {
                PrintWarn($"Cert override requested but not found in {location}/{storeName}.");
                return null;
            }

            var cert = found[0];
            if (!cert.HasPrivateKey)
            {
                PrintWarn($"Override cert has no private key: {cert.Subject} ({cert.Thumbprint}).");
                return null;
            }

            PrintInfo($"Using override client certificate: {cert.Subject} (Thumbprint={cert.Thumbprint})");
            return cert;
        }
        catch (Exception ex)
        {
            PrintWarn($"Failed to read certificate override: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// print banner.
    /// </summary>
    private static void WriteBanner()
    {
        Console.WriteLine();
        WithColor(ConsoleColor.Cyan, () =>
        {
            Console.WriteLine("════════════════════════════════════════════════════════════════════");
            Console.WriteLine("  Managed Identity + mTLS PoP Demo");
            Console.WriteLine("════════════════════════════════════════════════════════════════════");
        });
    }

    /// <summary>
    /// write AI-style hello message.
    /// </summary>
    private static void WriteAiHello()
    {
        Console.WriteLine();
        Console.WriteLine($"  {Glyphs.Bullet} Good Day, hope your day is going well.");
        Console.WriteLine($"  {Glyphs.Bullet} Here are some options for you for today's demo.");
        Console.WriteLine();
    }

    /// <summary>
    /// print menu.
    /// </summary>
    /// <param name="uamiClientId"></param>
    /// <param name="resourceUrl"></param>
    /// <param name="property"></param>
    /// <param name="showLogs"></param>
    /// <param name="showFullToken"></param>
    private static void PrintMenu(string uamiClientId, string resourceUrl, string property, bool showLogs, bool showFullToken)
    {
        WriteSection("Acquire Tokens", ConsoleColor.Green);
        WriteItem("  1   - Use SAMI → Token from IDP (force refresh)", ConsoleColor.Gray);
        WriteItem("  1a  - Use SAMI → Token from Cache", ConsoleColor.Gray);
        WriteItem("  2   - Use UAMI → Token from IDP (force refresh)", ConsoleColor.Gray);
        WriteItem("  2a  - Use UAMI → Token from Cache", ConsoleColor.Gray);

        WriteSection("Call Resource", ConsoleColor.Cyan);
        WriteItem("  3   - Use SAMI token + cert → Call resource", ConsoleColor.Gray);
        WriteItem("  4   - Use UAMI token + cert → Call resource", ConsoleColor.Gray);

        WriteSection("Display & Toggles", ConsoleColor.Yellow);
        WriteItem($"  F   - Toggle Full Token view (currently {(showFullToken ? "ON" : "OFF")})", ConsoleColor.Yellow);
        WriteItem($"  L   - Toggle MSAL logging (currently {(showLogs ? "ON" : "OFF")})", ConsoleColor.Yellow);

        WriteSection("Settings", ConsoleColor.Magenta);
        WriteItem($"  set-uami     - Change UAMI client id (current: {uamiClientId})", ConsoleColor.Magenta);
        WriteItem($"  set-resource - Change resource URL (current: {resourceUrl})", ConsoleColor.Magenta);
        WriteItem($"  set-prop     - Change single property to display (current: {property})", ConsoleColor.Magenta);

        WriteSection("System", ConsoleColor.White);
        WriteItem("  C / cls / clear - Clear screen", ConsoleColor.White);
        WriteItem("  M   - Maximize window", ConsoleColor.White);
        WriteItem("  Q   - Quit", ConsoleColor.Red);
    }

    /// <summary>
    /// print footer.
    /// </summary>
    private static void PrintFooter()
    {
        Console.WriteLine();
        WithColor(ConsoleColor.Cyan, () =>
        {
            Console.WriteLine("Done. Thanks!");
            Console.WriteLine("════════════════════════════════════════════════════════════════════");
        });
    }

    /// <summary>
    /// boxed title.
    /// </summary>
    /// <param name="title"></param>
    private static void Boxed(string title)
    {
        Console.WriteLine();
        WithColor(ConsoleColor.DarkCyan, () =>
        {
            Console.WriteLine("────────────────────────────────────────────────────────────────────");
            Console.WriteLine($"  {title}");
            Console.WriteLine("────────────────────────────────────────────────────────────────────");
        });
    }

    /// <summary>
    /// write a section header with color.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="color"></param>
    private static void WriteSection(string title, ConsoleColor color)
    {
        Console.WriteLine();
        WithColor(color, () => Console.WriteLine($"[{title}]"));
    }

    /// <summary>
    /// write an item line with color.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="color"></param>
    private static void WriteItem(string text, ConsoleColor color) =>
        WithColor(color, () => Console.WriteLine(text));

    /// <summary>
    /// with temporary console color.
    /// </summary>
    /// <param name="color"></param>
    /// <param name="action"></param>
    private static void WithColor(ConsoleColor color, Action action)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        try { action(); }
        finally { Console.ForegroundColor = old; }
    }

    /// <summary>
    /// print an info message.
    /// </summary>
    /// <param name="msg"></param>
    private static void PrintInfo(string msg) => Console.WriteLine($"[INFO] {msg}");
    /// <summary>
    /// print a warning message.
    /// </summary>
    /// <param name="msg"></param>
    private static void PrintWarn(string msg) => WithColor(ConsoleColor.Yellow, () => Console.WriteLine($"[WARN] {msg}"));
    /// <summary>
    /// print an error message.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="detail"></param>
    private static void PrintError(string title, string detail) =>
        WithColor(ConsoleColor.Red, () => Console.WriteLine($"[ERROR] {title}: {detail}"));

    /// <summary>
    /// Indent each line of a string with the given prefix.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    private static string Indent(string s, string prefix)
    {
        using var sr = new StringReader(s);
        var sb = new StringBuilder();
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            sb.Append(prefix).AppendLine(line);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Glyphs for console output (Unicode vs ASCII).
    /// </summary>
    private static class Glyphs
    {
        public static bool Unicode => Console.OutputEncoding.CodePage == 65001;
        public static string Check => Unicode ? "✔" : "OK";
        public static string Cross => Unicode ? "✖" : "X";
        public static string Warn => Unicode ? "⚠" : "!";
        public static string Bullet => Unicode ? "•" : "*";
        public static string[] SpinnerFrames => Unicode
            ? new[] { "⠋", "⠙", "⠸", "⠼", "⠴", "⠦", "⠇", "⠏" }   // Braille spinner
            : new[] { "-", "\\", "|", "/" };                     // ASCII spinner
    }

    /// <summary>
    /// Utility for showing an animated spinner during async work.
    /// </summary>
    private static class Ui
    {
        public static async Task<T> WithSpinnerAsync<T>(string message, Func<Task<T>> work, int intervalMs = 80)
        {
            var frames = Glyphs.SpinnerFrames;
            using var cts = new CancellationTokenSource();
            var spin = Task.Run(async () =>
            {
                var i = 0;
                while (!cts.IsCancellationRequested)
                {
                    var frame = frames[i++ % frames.Length];
                    Console.Write($"\r{frame} {message}   ");
                    try { await Task.Delay(intervalMs, cts.Token).ConfigureAwait(false); } catch { /* ignore */ }
                }
            });

            try
            {
                var result = await work().ConfigureAwait(false);
                cts.Cancel();
                Console.WriteLine($"\r{Glyphs.Check} {message}                ");
                return result;
            }
            catch
            {
                cts.Cancel();
                Console.WriteLine($"\r{Glyphs.Cross} {message}                ");
                throw;
            }
        }

        public static async Task WithSpinnerAsync(string message, Func<Task> work, int intervalMs = 80) =>
            await WithSpinnerAsync<object>(message, async () => { await work().ConfigureAwait(false); return new object(); }, intervalMs).ConfigureAwait(false);
    }

    /// <summary>
    /// toggleable MSAL logger for console output.
    /// </summary>
    private sealed class ToggleableLogger : IIdentityLogger
    {
        public bool Enabled { get; set; } = false;
        public EventLogLevel Level { get; set; } = EventLogLevel.Informational;
        public EventLogLevel MinLogLevel => Level;
        public bool IsEnabled(EventLogLevel eventLogLevel) => Enabled && eventLogLevel <= Level;
        public void Log(LogEntry entry)
        {
            if (!Enabled) return;
            Console.WriteLine(entry.Message);
        }
    }

    /// <summary>
    /// maximize console window (Windows only).
    /// </summary>
    /// <param name="silent"></param>
    private static void TryMaximizeConsoleWindow(bool silent)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                if (!silent) PrintWarn("Maximize is only supported on Windows; skipping.");
                return;
            }

            try
            {
                var targetWidth = Console.LargestWindowWidth;
                var targetHeight = Console.LargestWindowHeight;

                if (Console.BufferWidth < targetWidth) Console.BufferWidth = targetWidth;
                if (Console.BufferHeight < targetHeight) Console.BufferHeight = targetHeight;

                Console.SetWindowSize(targetWidth, targetHeight);
            }
            catch { /* ignore */ }

            IntPtr hWnd = GetConsoleWindow();
            if (hWnd != IntPtr.Zero) ShowWindow(hWnd, SW_MAXIMIZE);

            if (!silent) PrintInfo("Tried to maximize window (host terminal may limit this).");
        }
        catch
        {
            if (!silent) PrintWarn("Maximize not supported in this host.");
        }
    }

    /// <summary>
    /// Show security warning about full token display.
    /// </summary>
    private static void ShowFullTokenWarning()
    {
        WithColor(ConsoleColor.Yellow, () =>
        {
            Console.WriteLine();
            Console.WriteLine("  ⚠ SECURITY REMINDER: Full access tokens are highly sensitive.");
            Console.WriteLine("     • Avoid screenshots and screen sharing while visible.");
            Console.WriteLine("     • Do not paste into chats, tickets, or logs.");
            Console.WriteLine("     • Treat like a password/secret; clear the screen after use (C).");
            Console.WriteLine();
        });
    }
}

