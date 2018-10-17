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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AdalDesktopTestApp
{
    class Program
    {
        private static AppLogger AppLogger { get; } = new AppLogger();

        private const string ClientId = "<CLIENT_ID>";
        private const string RedirectUri = "https://ClientReplyUrl";
        private const string User = ""; // can also be empty string for testing IWA and U/P
        private const string Resource = "https://graph.windows.net";

        [STAThread]
        static void Main(string[] args)
        {
            LoggerCallbackHandler.LogCallback = AppLogger.Log;


            AuthenticationContext context = new AuthenticationContext("https://login.microsoftonline.com/common", true);

            if (ClientId == "<CLIENT_ID>")
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "Please confgure the app first!! Press any key to exit"));
                Console.Read();
                return;
            }

            RunAppAsync(context).Wait();
        }

        private static async Task RunAppAsync(AuthenticationContext context)
        {
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
                        1. Clear the cache
                        2. Acquire Token by Integrated Windows Auth
                        3. Acquire Token Interactively
                        4. Acquire Token with Username and Password
                        5. Acquire Token Silently
                        6. Acquire Token by Device Code 
                        0. Exit App
                    Enter your Selection: ");

                int.TryParse(Console.ReadLine(), out var selection);

                try
                {
                    Task<AuthenticationResult> authTask = null;
                    LoggerCallbackHandler.PiiLoggingEnabled = true;
                    switch (selection)
                    {
                        case 1:
                            // clear cache
                            context.TokenCache.Clear();
                            break;
                        case 2: // acquire token IWA
                            authTask = context.AcquireTokenAsync(Resource, ClientId, new UserCredential(User));
                            await FetchToken(authTask);
                            break;
                        case 3: // acquire token interactive
                            authTask = context.AcquireTokenAsync(Resource, ClientId, new Uri(RedirectUri), new PlatformParameters(PromptBehavior.Auto));
                            await FetchToken(authTask);
                            break;
                        case 4: // acquire token with username and password
                            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "Enter password for user {0} :", User));
                            authTask = context.AcquireTokenAsync(Resource, ClientId, new UserPasswordCredential(User, Console.ReadLine()));
                            await FetchToken(authTask);
                            break;
                        case 5: // acquire token silent
                            authTask = context.AcquireTokenSilentAsync(Resource, ClientId);
                            await FetchToken(authTask);
                            break;
                        case 6: // device code flow
                            authTask = GetTokenViaDeviceCodeAsync(context);
                            await FetchToken(authTask);
                            break;
                        case 0:
                            return;
                        default:
                            break;
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }

                Console.WriteLine("\n\nHit 'ENTER' to continue...");
                Console.ReadLine();
            }
        }

        private static async Task<AuthenticationResult> GetTokenViaDeviceCodeAsync(AuthenticationContext ctx)
        {
            AuthenticationResult result = null;

            try
            {
                DeviceCodeResult codeResult = await ctx.AcquireDeviceCodeAsync(Resource, ClientId);
                Console.ResetColor();
                Console.WriteLine("You need to sign in.");
                Console.WriteLine("Message: " + codeResult.Message + "\n");
                result = await ctx.AcquireTokenByDeviceCodeAsync(codeResult);
            }
            catch (Exception exc)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Something went wrong.");
                Console.WriteLine("Message: " + exc.Message + "\n");
            }
            return result;


        }

        private static async Task FetchToken(Task<AuthenticationResult> authTask)
        {
            await authTask;

            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Token is {0}", authTask.Result.AccessToken);
            Console.ResetColor();
        }

    }
}