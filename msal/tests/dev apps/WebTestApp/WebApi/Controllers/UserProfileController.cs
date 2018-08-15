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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using WebApi.Utils;

namespace WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class UserProfileController : Controller
    {
        private const string MsGraphMeQuery = "https://graph.microsoft.com/v1.0/me";
        private const string MsGraphUserReadScope = "User.Read";

        private static readonly StringBuilder LogStringBuilder = new StringBuilder();

        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        private const string UserCache = "UserCache";
        private const string ApplicationCache = "ApplicationCache";


        static UserProfileController()
        {
            Logger.LogCallback = delegate(LogLevel level, string message, bool containsPii)
            {
                lock (LogStringBuilder)
                {
                    LogStringBuilder.AppendLine("[" + level + "]" + " [hasPii - " + containsPii + "] - " +
                                                message);
                }
            };
            Logger.Level = LogLevel.Verbose;
            Logger.PiiLoggingEnabled = true;
        }

        private static void ClearLog()
        {
            lock (LogStringBuilder)
            {
                LogStringBuilder.Clear();
            }
        }

        private static string GetLog()
        {
            lock (LogStringBuilder)
            {
                return LogStringBuilder.ToString();
            }
        }

        private ConfidentialClientApplication GetConfidentialClient()
        {
            var userCache =
                MsalCacheHelper.GetMsalFileCacheInstance(Startup.Configuration["AzureAd:ClientId"] + "_" + GetCurrentUserId() +
                                                         "_" + UserCache);
            var appCache =
                MsalCacheHelper.GetMsalFileCacheInstance(Startup.Configuration["AzureAd:ClientId"] + "_" +
                                                         ApplicationCache);

            var confidentialClient = new ConfidentialClientApplication(
                Startup.Configuration["AzureAd:ClientId"],
                Startup.Configuration["AzureAd:TenantAuthority"],
                Startup.Configuration["AzureAd:RedirectUri"],
                new ClientCredential(Startup.Configuration["AzureAd:Secret"]),
                userCache, appCache);

            confidentialClient.SliceParameters = "slice=testslice";

            return confidentialClient;
        }

        private static string GetFormattedLog()
        {
            return Environment.NewLine + Environment.NewLine + Environment.NewLine + "Web Api MSAL Log:" +
                   Environment.NewLine + Environment.NewLine + GetLog();
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        }

        [HttpGet]
        public async Task<string> GetAsync()
        {
            await Semaphore.WaitAsync();
            try
            {
                ClearLog();

                var identity = User.Identity as ClaimsIdentity;
                var userAccessToken = identity?.BootstrapContext as string;

                var userAssertion = new UserAssertion(userAccessToken);

                string result;
                try
                {
                    var authResult =
                        await GetConfidentialClient().AcquireTokenOnBehalfOfAsync(new[] {MsGraphUserReadScope},
                            userAssertion);

                    result = await CallApiAsync(MsGraphMeQuery, authResult.AccessToken);
                }
                catch (MsalException ex)
                {
                    result = "Web Api failed, MSAL Exception - " + ex.Message;
                }
                catch (Exception ex)
                {
                    result = "Web Api failed, Exception - " + ex.Message;
                }

                return result + GetFormattedLog();
            }
            finally
            {
                Semaphore.Release();
            }
        }

        private static async Task<string> CallApiAsync(string apiUrl, string accessToken)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new Exception(response.StatusCode.ToString());

            return await response.Content.ReadAsStringAsync();
        }
    }
}
