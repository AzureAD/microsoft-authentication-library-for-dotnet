// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace WAMClassLibrary
{
    public class Authentication
    {
        /// <summary>
        /// Get the handle of the console window for Linux
        /// </summary>
        [DllImport("libX11")]
        private static extern IntPtr XOpenDisplay(string display);

        [DllImport("libX11")]
        private static extern IntPtr XRootWindow(IntPtr display, int screen);

        [DllImport("libX11")]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);


        public static async Task InvokeBrokerAsync()
        {
            IntPtr _parentHandle = XRootWindow(XOpenDisplay(null), 0);;
            Func<IntPtr> consoleWindowHandleProvider = () => _parentHandle;

            // 1. Configuration - read below about redirect URI
            var pca = PublicClientApplicationBuilder.Create("4b0db8c2-9f26-4417-8bde-3f0e3656f8e0"/*Lab Public Multi-Tenant*/)
                          .WithAuthority("https://login.microsoftonline.com/common")
                          .WithDefaultRedirectUri()
                          .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Linux){
                            ListOperatingSystemAccounts = true,
                            MsaPassthrough = true,
                            Title = "MSAL WSL Test App"
                          })
                          .WithParentActivityOrWindow(consoleWindowHandleProvider)
                          .WithLogging((x, y, z) => Console.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
                          .Build();

            // Add a token cache, see https://learn.microsoft.com/entra/msal/dotnet/how-to/token-cache-serialization?tabs=desktop

            // 2. GetAccounts
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            var accountToLogin = accounts.FirstOrDefault();

            try
            {
                var authResult = await pca.AcquireTokenSilent(new[] { "user.read" }, accountToLogin)
                                      .ExecuteAsync().ConfigureAwait(false);
            }
            catch (MsalUiRequiredException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.ErrorCode);
            }

            try
            {
                var authResult = await pca.AcquireTokenInteractive(new[] { "user.read" }).WithLoginHint("idlab@msidlab4.onmicrosoft.com")
                                      .ExecuteAsync().ConfigureAwait(false);

                Console.WriteLine(authResult.Account);

                Console.WriteLine("Acquired Token Successfully!!!");

            }
            catch (MsalClientException ex)
            {
                int errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
                Console.WriteLine("MsalClientException (ErrCode " + errorCode + "): " + ex.Message);
            }
            catch (MsalException ex)
            {
                Console.WriteLine($"MsalException Error signing-out user: {ex.Message}");
            }
            catch (Exception ex)
            {
                int errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
                Console.WriteLine("Error Acquiring Token (ErrCode " + errorCode + "): " + ex);
            }
            Console.Read();
        }

        public static void Main(string[] args)
        {
            InvokeBrokerAsync().Wait();
        }
    }
}
