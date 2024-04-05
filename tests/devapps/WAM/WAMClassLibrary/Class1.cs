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
using System.Windows.Forms;

namespace WAMClassLibrary
{
    public class Authentication
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        public static async Task InvokeBrokerAsync()
        {
            IntPtr _parentHandle = GetForegroundWindow();
            Func<IntPtr> consoleWindowHandleProvider = () => _parentHandle;

            // 1. Configuration - read below about redirect URI
            var pca = PublicClientApplicationBuilder.Create("04f0c124-f2bc-4f59-8241-bf6df9866bbd")
                          .WithAuthority(AzureCloudInstance.AzurePublic, "organizations")
                          .WithDefaultRedirectUri()
                          .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                          .WithParentActivityOrWindow(consoleWindowHandleProvider)
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
                var authResult = await pca.AcquireTokenInteractive(new[] { "user.read" })
                                      .ExecuteAsync().ConfigureAwait(false);

                Console.WriteLine(authResult.Account);

                Console.WriteLine("Acquired Token Successfully!!!");

                //logout
                IEnumerable<IAccount> account = await pca.GetAccountsAsync().ConfigureAwait(false);
                if (account.Any())
                {
                    try
                    {
                        await pca.RemoveAsync(account.FirstOrDefault()).ConfigureAwait(false);
                        Console.WriteLine("User has signed-out");
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
                }

                Console.Read();
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
    }
}
