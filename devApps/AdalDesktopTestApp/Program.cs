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
            while (true)
            {
                // display menu
                // clear display
                Console.Clear();

                Console.WriteLine("\n\t1. Acquire Token\n\t2. Acquire Token with Pii logging enabled\n\t" +
                                  "3. Acquire Token Conditional Access Policy\n\t0. Exit App");
                Console.WriteLine("\n\tEnter your Selection: ");

                int.TryParse(Console.ReadLine(), out var selection);

                switch (selection)
                {
                    case 1: // acquire token
                        try
                        {
                            LoggerCallbackHandler.PiiLoggingEnabled = false;
                            AcquireTokenAsync().Wait();
                        }
                        catch (AggregateException ae)
                        {
                            Console.WriteLine(ae.InnerException.Message);
                            Console.WriteLine(ae.InnerException.StackTrace);
                        }
                        break;
                    case 2: // acquire token with pii logging enabled
                        try
                        {
                            LoggerCallbackHandler.PiiLoggingEnabled = true;
                            AcquireTokenAsync().Wait();
                        }
                        catch (AggregateException ae)
                        {
                            Console.WriteLine(ae.InnerException.Message);
                            Console.WriteLine(ae.InnerException.StackTrace);
                        }
                        break;
                    case 3: // acquire token with claims
                        try
                        {
                            AcquireTokenWithClaimsAsync().Wait();
                        }
                        catch (AggregateException ae)
                        {
                            Console.WriteLine(ae.InnerException.Message);
                            Console.WriteLine(ae.InnerException.StackTrace);
                        }
                        break;
                    case 0:
                        return;
                    default:
                        break;
                }

                Console.WriteLine("\n\nHit 'ENTER' to continue...");
                Console.ReadLine();
            }
        }

        private static async Task AcquireTokenAsync()
        {
            AuthenticationContext context = new AuthenticationContext("https://login.microsoftonline.com/common", true);
            var result = await context.AcquireTokenAsync("https://graph.windows.net", "<CLIENT_ID>", new UserCredential("<USER>"));

            string token = result.AccessToken;
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

        private static async Task AcquireTokenWithClaimsAsync()
        {
            string claims = "{\"access_token\":{\"polids\":{\"essential\":true,\"values\":[\"5ce770ea-8690-4747-aa73-c5b3cd509cd4\"]}}}";

            AuthenticationContext context = new AuthenticationContext("https://login.microsoftonline.com/common", true);
            var result = await context.AcquireTokenAsync("https://graph.windows.net", "<CLIENT_ID>", 
                new Uri("<REDIRECT_URI>"), new PlatformParameters(PromptBehavior.Auto), new UserIdentifier("<USER>", UserIdentifierType.OptionalDisplayableId), null, claims).ConfigureAwait(false);

            string token = result.AccessToken;
            Console.WriteLine(token + "\n");
        }
    }
}
