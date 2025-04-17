// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.IdentityModel.Abstractions;

namespace MsiTokenRevocationSample
{
    class Program
    {
        // We build the ManagedIdentityApplication exactly once:
        private static IManagedIdentityApplication s_managedIdentityApp = null!;

        // Wait 1 second between spinner frames (just for demonstration)
        private const int SpinnerIntervalMs = 250;

        static async Task Main(string[] args)
        {
            PrintBanner();
            Console.WriteLine();

            // Create the IManagedIdentityApplication with "cp1" 
            // so we can handle CAE claims challenges.
            s_managedIdentityApp = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.WithUserAssignedClientId("04ca4d6a-c720-4ba1-aa06-f6634b73fe7a"))
                .WithClientCapabilities(new[] { "cp1" })
                .Build();

            // 1) Acquire the MSI token and retrieve the secret
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Step 1: Acquire Key Vault token via UAMI and retrieve the secret...");
            Console.ResetColor();

            var oldToken = await AcquireSecretFromKeyVaultAsync().ConfigureAwait(false);

            WaitWithSpinner("Awaiting next step...", 5); // “Wait” 5 seconds just for show

            // 2) Revoke the token
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nStep 2: Revoke the user-assigned MSI token using ARM + certificate...");
            Console.ResetColor();

            // Revoke 
            await RevokeUserAssignedMsiTokensWithCertAsync().ConfigureAwait(false);

            WaitWithSpinner("Token is revoked... waiting a bit for effect", 5);

            // 3) Poll Key Vault with old token until it fails,
            //    then reacquire with claims if needed
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nStep 3: Polling Key Vault with old (revoked) token until we see a failure...");
            Console.ResetColor();

            await PollKeyVaultWithOldTokenAsync(oldToken).ConfigureAwait(false);

            PrintConclusion();
            Console.ReadLine();
        }

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================");
            Console.WriteLine("       DEMO: MANAGED IDENTITY TOKEN REVOCATION    ");
            Console.WriteLine("==================================================");
            Console.ResetColor();
        }

        private static void PrintConclusion()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nAll done! Press ENTER to exit...");
            Console.ResetColor();
        }

        /// <summary>
        /// Simple spinner animation while we “wait” or do a fake pause
        /// </summary>
        private static void WaitWithSpinner(string message, int seconds)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n{message}");
            Console.ResetColor();

            var spinnerFrames = new[] { '|', '/', '-', '\\' };
            int frame = 0;
            int totalMs = seconds * 1000;
            int elapsed = 0;

            while (elapsed < totalMs)
            {
                Console.Write($"\r  {spinnerFrames[frame]} ");
                frame = (frame + 1) % spinnerFrames.Length;
                Thread.Sleep(SpinnerIntervalMs);
                elapsed += SpinnerIntervalMs;
            }
            Console.Write("\r   \n");
        }

        /// <summary>
        /// Acquire a Key Vault token using the shared _managedIdentityApp, 
        /// then retrieve the secret, and return the token.
        /// </summary>
        private static async Task<string> AcquireSecretFromKeyVaultAsync()
        {
            var token = string.Empty;

            try
            {
                var authResult = await s_managedIdentityApp
                    .AcquireTokenForManagedIdentity("https://vault.azure.net/.default")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                token = authResult.AccessToken;
                Console.WriteLine("  \u2713 Token for Key Vault acquired successfully!\n");

                // Create a regular HttpClient
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Retrieve a secret from Key Vault
                var keyVaultUri = "https://revoguardkeyvault.vault.azure.net/secrets/RevoGuardSecret?api-version=7.5";
                var response = await httpClient.GetAsync(keyVaultUri).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Key Vault request failed: {response.StatusCode}\n{content}");
                    Console.ResetColor();
                }
                else
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("value", out JsonElement secretValue))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  \u2713 Secret from Key Vault:");
                        Console.ResetColor();
                        Console.WriteLine($"    {secretValue.GetString()}");
                    }
                    else
                    {
                        Console.WriteLine("Could not find 'value' property in JSON response.");
                        Console.WriteLine(content);
                    }
                }
            }
            catch (MsalServiceException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"MSAL Service Exception: {e.ErrorCode}");
                PrintExceptionChain(e);
                Console.ResetColor();
            }

            return token;
        }

        /// <summary>
        /// Revoke user-assigned MSI tokens using a certificate 
        /// (with subject name "CN=LabAuth.MSIDLab.com") + Azure Resource Manager.
        /// </summary>
        private static async Task RevokeUserAssignedMsiTokensWithCertAsync()
        {
            try
            {
                // Retrieve the certificate by subject name, e.g. "CN=LabAuth.MSIDLab.com"
                string subjectName = "CN=LabAuth.MSIDLab.com";
                X509Certificate2? cert = FindCertificateBySubjectName(StoreLocation.CurrentUser, subjectName);

                if (cert == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Could not find certificate in CurrentUser\\My with subject name: {subjectName}");
                    Console.ResetColor();
                    return;
                }

                // Create a ConfidentialClientApplication to call ARM
                var app = ConfidentialClientApplicationBuilder
                    .Create("163ffef9-a313-45b4-ab2f-c7e2f5e0e23e")
                    .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                    .WithCertificate(cert, sendX5C: true)
                    .Build();

                var authResult = await app
                    .AcquireTokenForClient(new[] { "https://management.azure.com/.default" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var armToken = authResult.AccessToken;
                Console.WriteLine("  \u2713 Successfully acquired ARM token for revocation!\n");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", armToken);

                string subscriptionId = "ff71c235-108e-4869-9779-5f275ce45c44";
                string resourceGroupName = "RevoGuard";
                string userAssignedIdentityName = "RevokeUAMI";
                string apiVersion = "2023-07-31-PREVIEW";

                var getUrl = $"https://management.azure.com/subscriptions/{subscriptionId}" +
                             $"/resourceGroups/{resourceGroupName}" +
                             $"/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{userAssignedIdentityName}" +
                             $"?api-version={apiVersion}";

                var revokeUrl = $"https://management.azure.com/subscriptions/{subscriptionId}" +
                                $"/resourceGroups/{resourceGroupName}" +
                                $"/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{userAssignedIdentityName}" +
                                $"/revokeTokens?api-version={apiVersion}";

                Console.WriteLine("Getting identity info...");
                var getResponse = await httpClient.GetAsync(getUrl).ConfigureAwait(false);
                var getContent = await getResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine($"  GET status: {getResponse.StatusCode}");
                Console.WriteLine($"  {getContent}\n");

                Console.WriteLine("Revoking MSI tokens...");
                var postResponse = await httpClient.PostAsync(revokeUrl, null).ConfigureAwait(false);
                var postContent = await postResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine($"  POST status: {postResponse.StatusCode}");
                Console.WriteLine($"  {postContent}\n");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error when calling ARM:");
                Console.WriteLine(ex);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Poll Key Vault with the old token until it fails (401),
        /// then reacquire a new token if there's a claims challenge
        /// (or do a normal re-acquire if no claims).
        /// </summary>
        private static async Task PollKeyVaultWithOldTokenAsync(string oldToken)
        {
            if (string.IsNullOrEmpty(oldToken))
            {
                Console.WriteLine("No token to poll with. Exiting...");
                return;
            }

            int iteration = 0;
            while (true)
            {
                iteration++;
                Console.WriteLine($"\nAttempt #{iteration}: Using old token to call Key Vault again...");

                // We call Key Vault, capturing if there's any claims challenge
                var (stillValid, claims) = await CallKeyVaultOnceAsync(oldToken).ConfigureAwait(false);

                if (!stillValid)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Old token seems invalid now. Attempting to reacquire a new token...");
                    Console.ResetColor();

                    // Reacquire new token, possibly with .WithClaims(...)
                    var freshToken = await ReacquireMsiTokenWithClaimsAsync(claims).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(freshToken))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Got a fresh token, let's see if Key Vault call works now:");
                        Console.ResetColor();

                        // If success -> print "Status: OK - The new token is accepted!"
                        await CallKeyVaultOnceAsync(freshToken, printNewTokenAccepted: true).ConfigureAwait(false);
                    }
                    Console.WriteLine("\nStopping the poll loop.");
                    break;
                }

                WaitWithSpinner("Waiting 5 minutes for next attempt...", 5 * 60); // 5 minutes
            }
        }

        /// <summary>
        /// Makes a single call to Key Vault with the provided token.
        /// Returns (Success, ClaimsIfAny).
        /// 
        /// The optional 'printNewTokenAccepted' changes the success line from 
        /// "The token is still accepted!" to "The new token is accepted!".
        /// </summary>
        private static async Task<(bool Success, string? Claims)> CallKeyVaultOnceAsync(
            string token,
            bool printNewTokenAccepted = false)
        {
            var keyVaultUri = "https://revoguardkeyvault.vault.azure.net/secrets/RevoGuardSecret?api-version=7.5";
            string? claims = null;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await httpClient.GetAsync(keyVaultUri).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (printNewTokenAccepted)
                    {
                        Console.WriteLine($"Status: {response.StatusCode} - The new token is accepted!");
                    }
                    else
                    {
                        Console.WriteLine($"Status: {response.StatusCode} - The token is still accepted!");
                    }
                    Console.ResetColor();

                    return (true, null);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Status: {response.StatusCode} - Possibly revoked or invalid now.");
                    Console.ResetColor();

                    // Check for a claims challenge in the WWW-Authenticate header
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // MSAL method to get the claims challenge from the response headers
                        claims = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(response.Headers);

                        // Optionally print them
                        if (!string.IsNullOrEmpty(claims))
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"Extracted claims from header: {claims}");
                            Console.ResetColor();
                        }
                    }

                    Console.WriteLine(content);
                    return (false, claims);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error calling Key Vault with old token:");
                Console.WriteLine(ex);
                Console.ResetColor();
                return (false, null);
            }
        }

        /// <summary>
        /// Reacquires an MSI token from the *same* _managedIdentityApp, possibly with .WithClaims(...).
        /// </summary>
        private static async Task<string> ReacquireMsiTokenWithClaimsAsync(string? claims)
        {
            try
            {
                // We reuse the same _managedIdentityApp created in Main
                var builder = s_managedIdentityApp.AcquireTokenForManagedIdentity("https://vault.azure.net/.default");

                // If we have a claims challenge, include it
                if (!string.IsNullOrEmpty(claims))
                {
                    Console.WriteLine($"Using .WithClaims(...) due to challenge:\n{claims}\n");
                    builder = builder.WithClaims(claims);
                }

                var authResult = await builder.ExecuteAsync().ConfigureAwait(false);
                return authResult.AccessToken;
            }
            catch (MsalServiceException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"MSAL Exception reacquiring token: {e.ErrorCode}");
                Console.WriteLine(e.Message);
                Console.ResetColor();
                return string.Empty;
            }
        }

        /// <summary>
        /// Finds a certificate by subject name in the specified store location (LocalMachine or CurrentUser).
        /// For example, subjectName = "CN=LabAuth.MSIDLab.com"
        /// </summary>
        private static X509Certificate2? FindCertificateBySubjectName(StoreLocation storeLocation, string subjectName)
        {
            using var store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                if (cert?.SubjectName?.Name?.Equals(subjectName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return cert;
                }
            }
            return null;
        }

        private static void PrintExceptionChain(Exception ex)
        {
            Exception? current = ex;
            while (current != null)
            {
                Console.WriteLine($"Exception Message: {current.Message}");
                Console.WriteLine($"Exception StackTrace: {current.StackTrace}");
                current = current.InnerException;
            }
        }
    }

    /// <summary>
    /// A simple example logger for identity events. 
    /// Disabled by default. Enabled for debugging.
    /// </summary>
    class IdentityLogger : IIdentityLogger
    {
        public EventLogLevel MinLogLevel { get; }

        /// <summary>
        /// Enable for debugging
        /// </summary>
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
            Console.WriteLine(entry.Message);
        }
    }
}
