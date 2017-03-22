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
using Microsoft.Identity.Client;
using Test.MSAL.Common;
using TestApp.PCL;

namespace DesktopTestApp
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


        public static async Task<IAuthenticationResult> GetTokenSilentAsync(User user)
        {
            TokenBroker brkr = new TokenBroker();
            PublicClientApplication app =
                new PublicClientApplication("<client_id>");
            try
            {
                return await app.AcquireTokenSilentAsync(brkr.Sts.ValidScope, null);
            }
            catch (Exception ex)
            {
                string msg = ex.Message + "\n" + ex.StackTrace;
                Console.WriteLine(msg);
                return await app.AcquireTokenAsync(brkr.Sts.ValidScope, user.DisplayableId, UIBehavior.SelectAccount, null);
            }
            
        }

        public static async Task<IAuthenticationResult> GetTokenInteractiveAsync()
        {
            try
            {
                TokenBroker brkr = new TokenBroker();
                PublicClientApplication app =
                    new PublicClientApplication("<client_id>");
                await app.AcquireTokenAsync(brkr.Sts.ValidScope);

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
                PublicClientApplication app = new PublicClientApplication(Sts.Authority, "<client_id>");
                var result = await app.AcquireTokenAsync(Sts.ValidScope);
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
