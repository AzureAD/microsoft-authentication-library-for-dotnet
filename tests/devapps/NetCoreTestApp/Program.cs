//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.DevAppsTelemetry;

namespace NetCoreTestApp
{
    public class Program
    {
        private readonly static string ClientIdForPublicApp = "0615b6ca-88d4-4884-8729-b178178f7c27";
        private readonly static string ClientIdForConfidentialApp = "<enter id>";

        private readonly static string Username = ""; // used for WIA and U/P, cannot be empty on .net core
        private readonly static string Authority = "https://login.microsoftonline.com/organizations/v2.0"; // common will not work for WIA and U/P but it is a good test case
        private readonly static IEnumerable<string> Scopes = new[] { "user.read" }; // used for WIA and U/P, can be empty

        private const string GraphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        public static void Main(string[] args)
        {

            PublicClientApplication pca = new PublicClientApplication(
                ClientIdForPublicApp,
                Authority,
                TokenCacheHelper.GetUserCache()); // token cache serialization https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/token-cache-serialization

            Logger.LogCallback = Log;
            Logger.Level = LogLevel.Verbose;
            Logger.PiiLoggingEnabled = true;

            Telemetry.GetInstance().RegisterReceiver(new ClientTelemetryHandler().OnEvents);
            RunConsoleAppLogicAsync(pca).Wait();
        }

        private static async Task RunConsoleAppLogicAsync(PublicClientApplication pca)
        {
            while (true)
            {
                Console.Clear();

                await DisplayAccountsAsync(pca).ConfigureAwait(false);

                // display menu
                Console.WriteLine(@"
                        1. Acquire Token by Windows Integrated Auth
                        2. Acquire Token with Username and Password
                        3. Acquire Token with Device Code
                        4. Acquire Token Silently
                        5. Confidential Client with Certificate (needs extra config)
                        6. Clear PCA cache
                        0. Exit App
                    Enter your Selection: ");
                int.TryParse(Console.ReadLine(), out var selection);

                Task<AuthenticationResult> authTask = null;

                try
                {
                    switch (selection)
                    {
                        case 1: // acquire token
                            authTask = pca.AcquireTokenByIntegratedWindowsAuthAsync(Scopes, Username);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;
                        case 2: // acquire token u/p
                            SecureString password = GetPasswordFromConsole();
                            authTask = pca.AcquireTokenByUsernamePasswordAsync(Scopes, Username, password);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;
                        case 3:
                            authTask = pca.AcquireTokenWithDeviceCodeAsync(
                                Scopes,
                                deviceCodeResult =>
                                {
                                    Console.WriteLine(deviceCodeResult.Message);
                                    return Task.FromResult(0);
                                });
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;
                        case 4: // acquire token silent
                            IAccount account = pca.GetAccountsAsync().Result.FirstOrDefault();
                            if (account == null)
                            {
                                Log(LogLevel.Error, "Test App Message - no accounts found, AcquireTokenSilentAsync will fail... ", false);
                            }

                            authTask = pca.AcquireTokenSilentAsync(Scopes, account);
                            await FetchTokenAndCallGraphAsync(pca, authTask).ConfigureAwait(false);

                            break;
                        case 5:
                            RunClientCredentialWithCertificate();
                            break;
                        case 6:
                            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                            foreach (var acc in accounts)
                            {
                                await pca.RemoveAsync(acc).ConfigureAwait(false);
                            }

                            break;
                        case 0:
                            return;
                        default:
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, ex.Message, false);
                    Log(LogLevel.Error, ex.StackTrace, false);
                }

                Console.WriteLine("\n\nHit 'ENTER' to continue...");
                Console.ReadLine();
            }
        }

        private static async Task FetchTokenAndCallGraphAsync(PublicClientApplication pca, Task<AuthenticationResult> authTask)
        {
            await authTask.ConfigureAwait(false);

            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Token is {0}", authTask.Result.AccessToken);
            Console.ResetColor();


            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            await DisplayAccountsAsync(pca).ConfigureAwait(false);
            var callGraphTask = CallGraphAsync(authTask.Result.AccessToken);
            callGraphTask.Wait();
            Console.WriteLine("Result from calling the ME endpoint of the graph: " + callGraphTask.Result);
            Console.ResetColor();
        }

        private static void RunClientCredentialWithCertificate()
        {
            ClientCredential cc = new ClientCredential(new ClientAssertionCertificate(GetCertificateByThumbprint("<THUMBPRINT>")));
            ConfidentialClientApplication app = new ConfidentialClientApplication(ClientIdForConfidentialApp, "http://localhost", cc, new TokenCache(), new TokenCache());
            try
            {
                AuthenticationResult result = app.AcquireTokenForClientAsync(new string[] { "User.Read.All" }, true).Result;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            finally { Console.ReadKey(); }
        }

        private static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (certs.Count > 0)
                {
                    return certs[0];
                }
                throw new InvalidOperationException($"Cannot find certificate with thumbprint '{thumbprint}'");
            }
        }


        private static async Task DisplayAccountsAsync(PublicClientApplication pca)
        {
            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "For the public client, the tokenCache contains {0} token(s)", accounts.Count()));

            foreach (var account in accounts)
            {
                Console.WriteLine("PCA account for: " + account.Username + "\n");
            }
        }

        private static void Log(LogLevel level, string message, bool containsPii)
        {
            if (!containsPii)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
            }

            switch (level)
            {
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                default:
                    break;
            }

            Console.WriteLine($"{level} {message}");
            Console.ResetColor();
        }

        private static SecureString GetPasswordFromConsole()
        {
            Console.Write("Password: ");
            var pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (i.KeyChar != '\u0000') // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }
            return pwd;
        }

        private static async Task<string> CallGraphAsync(string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, GraphAPIEndpoint);
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }


    }
}
