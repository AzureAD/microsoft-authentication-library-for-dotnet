//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Identity.Client;
using Test.MSAL.Common;
using TestApp.PCL;

namespace AdalDesktopTestApp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                var task = GetTokenInteractiveAsync();
                Task.WaitAll(task);
                var result = task.Result;
                Console.WriteLine(result.AccessToken);

                task = GetTokenSilentAsync(result.User);
                Task.WaitAll(task);
                result = task.Result;

                Console.WriteLine(result.AccessToken);
            }
            catch (AggregateException ae)
            {
                Console.WriteLine(ae.InnerException.Message);
                Console.WriteLine(ae.InnerException.StackTrace);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        private static async Task AcquireTokenAsync()
        {
            TokenBroker app = new TokenBroker();
            string token = await GetTokenIntegratedAuthAsync(app.Sts).ConfigureAwait(false);
            Console.WriteLine(token);
        }


        public static async Task<AuthenticationResult> GetTokenSilentAsync(User user)
        {
            TokenBroker brkr = new TokenBroker();
            PublicClientApplication app =
                new PublicClientApplication("https://login.microsoftonline.com/msdevex.onmicrosoft.com",
                    "7c7a2f70-caef-45c8-9a6c-091633501de4");
            try
            {
                app.PlatformParameters = new PlatformParameters();
                return await app.AcquireTokenSilentAsync(brkr.Sts.ValidScope);
            }
            catch (Exception ex)
            {
                string msg = ex.Message + "\n" + ex.StackTrace;
                Console.WriteLine(msg);
                return await app.AcquireTokenAsync(brkr.Sts.ValidScope, user.DisplayableId, UiOptions.UseCurrentUser, null);
            }
            
        }

        public static async Task<AuthenticationResult> GetTokenInteractiveAsync()
        {
            try
            {
                TokenBroker brkr = new TokenBroker();
                PublicClientApplication app = new PublicClientApplication(brkr.Sts.Authority, "7c7a2f70-caef-45c8-9a6c-091633501de4");
                app.PlatformParameters = new PlatformParameters();
                return await app.AcquireTokenAsync(brkr.Sts.ValidScope);
            }
            catch (Exception ex)
            {
                string msg = ex.Message + "\n" + ex.StackTrace;
                Console.WriteLine(msg);
            }

            return null;
        }

        public static async Task<string> GetTokenIntegratedAuthAsync(Sts Sts)
        {
            try
            {
                PublicClientApplication app = new PublicClientApplication(Sts.Authority, "7c7a2f70-caef-45c8-9a6c-091633501de4");
                var result = await app.AcquireTokenWithIntegratedAuthAsync(Sts.ValidScope);
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                string msg = ex.Message + "\n" + ex.StackTrace;

                return msg;
            }
        }
    }
}
