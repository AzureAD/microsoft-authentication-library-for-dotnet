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
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AdalDesktopTestApp
{
    class Program
    {
        public static IPlatformParameters Parameters { get; set; }
        private static readonly AppLogger AppLogger = new AppLogger();

        [STAThread]
        static void Main(string[] args)
        {
            LoggerCallbackHandler.LogCallback = AppLogger.Log;
            string resource = "https://graph.windows.net";
            string clientId = "<CLIENT_ID>";
            string redirectUri = "<REDIRECT_URI>";
            string user = "<USER>";
            AuthenticationContext context = new AuthenticationContext("https://login.microsoftonline.com/common", true);
            while (true)
            {
                Console.Clear();

                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "TokenCache contains {0} token(s)", context.TokenCache.Count));
                foreach (var item in context.TokenCache.ReadItems())
                {
                    Console.WriteLine("  Cache item available for: " + item.DisplayableId + "\n");
                }

                // display menu
                Console.WriteLine(@"
                        1. Acquire Token by Windows Integrated Auth
                        2. Acquire Token by Windows Integrated Auth, with Pii logging enabled
                        3. Acquire Token Conditional Access Policy
                        4. Acquire Token Interactively
                        5. Acquire Token with Username and Password
                        9. Acquire Token Silently
                        0. Exit App
                    Enter your Selection: ");
                int.TryParse(Console.ReadLine(), out var selection);

                try
                {
                    Task<AuthenticationResult> task = null;
                    LoggerCallbackHandler.PiiLoggingEnabled = false;
                    switch (selection)
                    {
                        case 1: // acquire token
                            task = context.AcquireTokenAsync(resource, clientId, new UserCredential(user));
                            break;
                        case 2: // acquire token with pii logging enabled
                            LoggerCallbackHandler.PiiLoggingEnabled = true;
                            task = context.AcquireTokenAsync(resource, clientId, new UserCredential(user));
                            break;
                        case 3: // acquire token with claims
                            string claims = "{\"access_token\":{\"polids\":{\"essential\":true,\"values\":[\"5ce770ea-8690-4747-aa73-c5b3cd509cd4\"]}}}";
                            task = context.AcquireTokenAsync(resource, clientId, new Uri(redirectUri), new PlatformParameters(PromptBehavior.Auto),
                                new UserIdentifier(user, UserIdentifierType.OptionalDisplayableId), null, claims);
                            break;
                        case 4: // acquire token interactive
                            task = context.AcquireTokenAsync(resource, clientId, new Uri(redirectUri), new PlatformParameters(PromptBehavior.Auto));
                            break;
                        case 5: // acquire token with username and password
                            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "Enter password for user {0} :", user));
                            task = context.AcquireTokenAsync(resource, clientId, new UserPasswordCredential(user, Console.ReadLine()));
                            break;
                        case 9: // acquire token silent
                            task = context.AcquireTokenSilentAsync(resource, clientId);
                            break;
                        case 0:
                            return;
                        default:
                            break;
                    }
                    task.Wait();
                    string token = task.Result.AccessToken;
                    string logMessage = "\n\n" + "Pii Logging Enabled: " +
                                        LoggerCallbackHandler.PiiLoggingEnabled + "\n\n" +
                                        AppLogger.GetAdalLogs();

                    if (!LoggerCallbackHandler.PiiLoggingEnabled)
                    {
                        Console.WriteLine(token + "\n\n" + "====ADAL Logs====" + logMessage);
                    }
                    else
                    {
                        Console.WriteLine(token + "\n\n" + "====ADAL Logs Pii Enabled====" + logMessage);
                    }

                }
                catch (AggregateException ae)
                {
                    Console.WriteLine(ae.InnerException.Message);
                    Console.WriteLine(ae.InnerException.StackTrace);
                }

                Console.WriteLine("\n\nHit 'ENTER' to continue...");
                Console.ReadLine();
            }
        }
    }
}