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
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

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
                AcquireTokenAsync().Wait();
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
            string[] tenantAuthority = {"https://login.microsoftonline.com/fabrikam.com", "https://login.microsoftonline.com/megadeth.com", "https://login.microsoftonline.com/starbucks.com" };
            string[] scope = {"Mail.Read"};
            PublicClientApplication app = new PublicClientApplication();
            AuthenticationResult result = await app.AcquireTokenAsync(scope, "ceo@contoso.com");

            foreach (var authority in tenantAuthority)
            {
                result = await app.AcquireTokenSilentAsync(scope, result.User, authority);
                ReadMail(result.AccessToken);
            }
        }

        private static void ReadMail(string accessToken)
        {
            throw new NotImplementedException();
        }
    }
}
