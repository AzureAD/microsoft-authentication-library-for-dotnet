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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AdalCoreCLRTestApp
{
    class Program
    {
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
            AuthenticationContext context = new AuthenticationContext("https://login.microsoftonline.com/common", true);
            var certificate = GetCertificateByThumbprint("<CERT_THUMBPRINT>");
            var result = await context.AcquireTokenAsync("https://graph.windows.net", new ClientAssertionCertificate("<CLIENT_ID>", certificate));

            string token = result.AccessToken;
            Console.WriteLine(token + "\n");
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
                throw new Exception($"Cannot find certificate with thumbprint '{thumbprint}'");
            }
        }
    }
}