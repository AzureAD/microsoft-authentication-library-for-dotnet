using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace AppServiceTokenRevocation.Controllers
{
    public class RevokeController : Controller
    {
        private readonly ILogger<RevokeController> _logger;
        private readonly IHttpClientFactory _httpFactory;

        public RevokeController(ILogger<RevokeController> logger,
                                IHttpClientFactory httpFactory)
        {
            _logger = logger;
            _httpFactory = httpFactory;
        }

        // GET /Revoke/Run
        public async Task<IActionResult> Run(
            string subscriptionId = "ff71c235-108e-4869-9779-5f275ce45c44",
            string resourceGroupName = "RevoGuard",
            string identityName = "RevokeUAMI",
            string certThumbprint = "6714577625654a9d869767b161b8e0226ca37305",
            string tenantId = "bea21ebe-8b64-4d06-9f6d-6a889b120a7c",
            string clientId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e",
            string apiVersion = "2023-07-31-PREVIEW")
        {
            string getStatus = string.Empty;
            string getContent = string.Empty;
            string revokeStatus = string.Empty;
            string revokeContent = string.Empty;
            string errorMessage = string.Empty;

            try
            {
                // 1. Locate the client certificate in CurrentUser\My
                X509Certificate2? cert = FindCertificateByThumbprint(certThumbprint);

                if (cert == null)
                    throw new InvalidOperationException(
                        $"Cert with subject '{certThumbprint}' not found.");

                // 2. Acquire an ARM token with the cert
                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                    .WithCertificate(cert, sendX5C: true)
                    .Build();

                AuthenticationResult armResult = await cca
                    .AcquireTokenForClient(new[] { "https://management.azure.com/.default" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                string armToken = armResult.AccessToken;

                // 3. Call ARM
                string baseUrl = $"https://management.azure.com/subscriptions/{subscriptionId}" +
                                 $"/resourceGroups/{resourceGroupName}" +
                                 $"/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identityName}";

                using HttpClient http = _httpFactory.CreateClient();
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", armToken);

                //   3a. GET identity (optional – shows current etag etc.)
                string getUrl = $"{baseUrl}?api-version={apiVersion}";
                HttpResponseMessage getResp = await http.GetAsync(getUrl).ConfigureAwait(false);

                getStatus = $"{(int)getResp.StatusCode} {getResp.StatusCode}";
                getContent = await getResp.Content.ReadAsStringAsync().ConfigureAwait(false);

                //   3b. POST revokeTokens
                string revokeUrl = $"{baseUrl}/revokeTokens?api-version={apiVersion}";
                HttpResponseMessage postResp = await http.PostAsync(revokeUrl, null).ConfigureAwait(false);

                revokeStatus = $"{(int)postResp.StatusCode} {postResp.StatusCode}";
                revokeContent = await postResp.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                _logger.LogError(ex, "Token revocation failed.");
            }

            // Pass everything to the view
            ViewBag.GetStatus = getStatus;
            ViewBag.GetContent = getContent;
            ViewBag.RevokeStatus = revokeStatus;
            ViewBag.RevokeContent = revokeContent;
            ViewBag.Error = errorMessage;

            return View("Revoke");           
        }

        private static X509Certificate2? FindCertificateByThumbprint(string thumbprint)
        {
            foreach (var loc in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
            {
                using var store = new X509Store(StoreName.My, loc);
                store.Open(OpenFlags.ReadOnly);

                var certs = store.Certificates.Find(
                                X509FindType.FindByThumbprint,
                                thumbprint,
                                validOnly: false);

                if (certs.Count > 0)
                    return certs[0];
            }
            return null;
        }
    }
}
