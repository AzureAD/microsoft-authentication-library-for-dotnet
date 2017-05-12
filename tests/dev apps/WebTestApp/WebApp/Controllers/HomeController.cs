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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using WebApp.Utils;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private const string msGraphMeQuery = "https://graph.microsoft.com/v1.0/me";
        private const string msGraphUsersQuery = "https://graph.microsoft.com/v1.0/users";

        private const string msGraphScope = "https://graph.microsoft.com/.default";

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error(string message)
        {
            ViewBag.Message = message;
            return View("~/Views/Shared/Error.cshtml");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CallGraph()
        {
            var userName = User.FindFirst("preferred_username")?.Value;

            var authenticationResult = await ConfidentialClientUtils.AcquireTokenSilentAsync (Startup.Scopes, userName, HttpContext.Session);

            // Query for list of users in the tenant
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, msGraphMeQuery);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new Exception(response.StatusCode.ToString());

            // Record users in the data store (note that this only records the first page of users)
            var resultJson = await response.Content.ReadAsStringAsync();
            // MsGraphUserListResponse users = JsonConvert.DeserializeObject<MsGraphUserListResponse>(json);

            return View("~/Views/Home/Index.cshtml", resultJson);
        }

        private const string adminConsentUrlFormat =
            "https://login.microsoftonline.com/{0}/adminconsent?client_id={1}&redirect_uri={2}";
         
        [Authorize]
        [HttpGet]
        public ActionResult RequestPermissions()
        {
            return new RedirectResult(
                string.Format(adminConsentUrlFormat,
                    Startup.Configuration["AzureAd:Tenant"],
                    Startup.Configuration["AzureAd:ClientId"],
                    Startup.Configuration["AzureAd:AdminConsentRedirectUri"]
                    ));
        }

        [Authorize]
        [HttpGet]
        public ActionResult AdminConsent(string admin_consent, string tenant, string error, string error_description)
        {
            // If there was an error getting permissions from the admin. ask for permissions again
            if (error != null)
            {
                ViewBag.ErrorDescription = error_description;
                return View("~/Views/Home/Index.cshtml", "failed to grant permissions, error_description - " + error_description);
            }
            // If the admin successfully granted permissions, continue to showing the list of users
            else if (admin_consent == "True" && tenant != null)
            {
                return View("~/Views/Home/Index.cshtml", "admin successfully granted permissions");
            }
            return View("~/Views/Home/Index.cshtml");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CallGraphSecretClientCredential()
        {
           return await CallGraphClientCredential(new Microsoft.Identity.Client.ClientCredential(Startup.Configuration["AzureAd:ClientSecret"]));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CallGraphCertClientCredential()
        {
            return await CallGraphClientCredential(new Microsoft.Identity.Client.ClientCredential(Startup.Configuration["AzureAd:ClientCertificateThumbprint"]));
        }

        private async Task<IActionResult> CallGraphClientCredential(Microsoft.Identity.Client.ClientCredential clientCredential)
        {
            var tenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
            var authorityFormat = "https://login.microsoftonline.com/{0}/v2.0";
            var msGraphScope = "https://graph.microsoft.com/.default";
            var msGraphQuery = "https://graph.microsoft.com/v1.0/users";

            string resultJson;
            try
            {
                var authenticationResult =
                    await ConfidentialClientUtils.AcquireTokenForClientAsync(new[] {msGraphScope}, HttpContext.Session);

                // Query for list of users in the tenant, to ensure we have been granted the necessary permissions
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, msGraphQuery);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
                var response = await client.SendAsync(request);

                // If we get back a 403, we need to ask the admin for permissions
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    // daemonClient.AppTokenCache.Clear(Startup.clientId);
                    // return new RedirectResult("/Account/GrantPermissions");
                }
                else if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Status code for call to Graph is not ok");
                }

                resultJson = await response.Content.ReadAsStringAsync();
            }
            catch (MsalException ex)
            {
                // If we can't get a token, we need to ask the admin for permissions as well
                if (ex.ErrorCode == "failed_to_acquire_token_silently")
                    return new RedirectResult("/Account/GrantPermissions");

                return View("Error");
            }
            catch (Exception ex)
            {
                return View("Error");
            }

            return View("~/Views/Home/Index.cshtml", resultJson);
        }


    }
}
