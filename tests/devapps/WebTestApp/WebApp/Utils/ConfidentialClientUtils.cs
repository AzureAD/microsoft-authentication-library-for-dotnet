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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace WebApp.Utils
{
    public static class ConfidentialClientUtils
    {
        private const string UserCache = "UserCache";
        private const string ApplicationCache = "ApplicationCache";

        private static ConfidentialClientApplication CreateConfidentialClient(ClientCredential clientCredential, string authority, string userId,
            ISession session)
        {
            var userCache = MsalCacheHelper.GetMsalFileCacheInstance(Startup.ClientId + "_" + userId + "_" + UserCache);
            var appCache = MsalCacheHelper.GetMsalFileCacheInstance(Startup.ClientId + "_" + ApplicationCache);

            return new ConfidentialClientApplication(
                Startup.Configuration["AzureAd:ClientId"],
                authority,
                Startup.Configuration["AzureAd:RedirectUri"],
                clientCredential,
                userCache, appCache);
        }

        public static ClientCredential CreateSecretClientCredential()
        {
            return new ClientCredential(Startup.Configuration["AzureAd:ClientSecret"]);
        }

        public static ClientCredential CreateClientCertificateCredential()
        {
            return new ClientCredential(
                GetCertCredentialCert(Startup.Configuration["AzureAd:ClientCertificateThumbprint"]));
        }

        private static ClientAssertionCertificate GetCertCredentialCert(string thumbprint)
        {
            using (var store = new X509Store())
            {
                store.Open(OpenFlags.ReadOnly);
                // Place all certificates in an X509Certificate2Collection object.
                var certCollection = store.Certificates;
                // Find unexpired certificates.
                var currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                // From the collection of unexpired certificates, find the ones with the correct name.
                var signingCert = currentCerts.Find(X509FindType.FindByThumbprint, thumbprint, false);

                if (signingCert.Count == 0)
                {
                    return null;
                }

                var cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).First();
                return new ClientAssertionCertificate(cert);
            }
        }

        public static async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode,
            IEnumerable<string> scopes, ISession session, ClientCredential clientCredential, string userId)
        {
            var confidentialClient = GetConfidentialClientWithExtraParams(clientCredential, Startup.Configuration["AzureAd:CommonAuthority"], userId, session);

            return await confidentialClient.AcquireTokenByAuthorizationCodeAsync(authorizationCode, scopes).ConfigureAwait(false);
        }

        public static async Task<AuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, string userName, ISession session, 
            ClientCredential clientCredential, string userId)
        {
            var confidentialClient = GetConfidentialClientWithExtraParams(clientCredential, Startup.Configuration["AzureAd:CommonAuthority"], userId, session);
            var accounts = await confidentialClient.GetAccountsAsync().ConfigureAwait(false);
            var account = accounts.FirstOrDefault(u => u.Username.Equals(userName, StringComparison.OrdinalIgnoreCase));

            return await confidentialClient.AcquireTokenSilentAsync(scopes, account).ConfigureAwait(false);
        }

        public static async Task<AuthenticationResult> AcquireTokenForClientAsync(IEnumerable<string> scopes,
            ISession session, ClientCredential clientCredential, string userId)
        {
            var appAuthority = string.Format(
                CultureInfo.InvariantCulture,
                Startup.Configuration["AzureAd:AuthorityForApplication"], 
                Startup.Configuration["AzureAd:Tenant"]);
            var confidentialClient = GetConfidentialClientWithExtraParams(clientCredential, appAuthority, userId, session);

            return await confidentialClient.AcquireTokenForClientAsync(scopes).ConfigureAwait(false);
        }

        private static IConfidentialClientApplication GetConfidentialClientWithExtraParams(ClientCredential clientCredential, string authority, string userId, ISession session)
        {
            var confidentialClient = CreateConfidentialClient(clientCredential, authority, userId, session);

            var extraParamsStr = "";
            foreach (var entry in Startup.ExtraParamsDictionary)
            {
                if (!string.IsNullOrEmpty(extraParamsStr))
                {
                    extraParamsStr += "&";
                }
                extraParamsStr += entry.Key + "=" + entry.Value;
            }

            confidentialClient.SliceParameters = extraParamsStr;

            return confidentialClient;
        }
    }
}
