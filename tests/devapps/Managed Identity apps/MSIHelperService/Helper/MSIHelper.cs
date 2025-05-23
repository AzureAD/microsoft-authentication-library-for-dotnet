// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace MSIHelperService.Helper
{
    internal static class MSIHelper
    {
        //content type
        internal const string ContentTypeJson = "application/json";
        internal const string ContentTypeTextOrHtml = "text/html";
        internal const string ContentTypeMulipartOrFormData = "multipart/form-data";

        //default azure resource if nothing is passed in the controllers
        internal const string DefaultAzureResource = "WebApp";

        //IDENTITY_HEADER in the App Service
        internal const string ManagedIdentityAuthenticationHeader = "X-IDENTITY-HEADER";

        //Environment variables
        internal static readonly string? s_requestAppID = Environment.GetEnvironmentVariable("requestAppID");
        internal static readonly string? s_requestAppSecret = Environment.GetEnvironmentVariable("requestAppSecret");
        internal static readonly string? s_webAppCertThumbprint = Environment.GetEnvironmentVariable("WebAppCertThumbprint");

        //Microsoft authority endpoint
        internal const string Authority = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47";

        //Azure Resources
        internal enum AzureResource
        {
            WebApp,
            Function,
            VM,
            AzureArc,
            ServiceFabric,
            CloudShell
        }

        /// <summary>
        /// Gets the Environment Variables from the Azure Web App
        /// </summary>
        /// <param name="logger"></param>
        /// <returns>Returns the environment variables</returns>
        public static async Task<Dictionary<string, string>> GetWebAppEnvironmentVariablesAsync(
            ILogger logger)
        {
            //Gets Azure Web App Specific environment variables and sends it back
            //Sending back the specific ones that is needed for the MSI tests
            Dictionary<string, string>? keyValuePairs = new();
            keyValuePairs.Add("IDENTITY_HEADER", Environment.GetEnvironmentVariable("IDENTITY_HEADER") ?? "");
            keyValuePairs.Add("IDENTITY_ENDPOINT", Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT") ?? "");
            keyValuePairs.Add("IDENTITY_API_VERSION", Environment.GetEnvironmentVariable("IDENTITY_API_VERSION") ?? "");

            logger.LogInformation("GetWebAppEnvironmentVariables Function called.");

            return await Task.FromResult(keyValuePairs).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the MSI Token from the Azure Web App
        /// </summary>
        /// <param name="identityHeader"></param>
        /// <param name="uri"></param>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <returns>Returns MSI Token</returns>
        public static async Task<ActionResult?> GetWebAppMSIToken(
            string? identityHeader,
            string? uri,
            HttpClient httpClient,
            ILogger logger)
        {
            logger.LogInformation("GetWebAppMSIToken Function called.");

            var decodedUri = HttpUtility.UrlDecode(uri);

            //set the http get method and the required headers for a web app
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, decodedUri);
            requestMessage.Headers.Add(ManagedIdentityAuthenticationHeader, identityHeader);

            //clear the default request header for each call
            ClearDefaultRequestHeaders(logger, httpClient);

            //send the request
            HttpResponseMessage? result = await httpClient
                .SendAsync(requestMessage)
                .ConfigureAwait(false);

            string body = await result.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            logger.LogInformation("GetWebAppMSIToken Function call was successful.");

            return GetContentResult(body, "application/json", (int)result.StatusCode);
        }

        /// <summary>
        /// Clear default request headers on the http client
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="httpClient"></param>
        /// <returns></returns>
        private static void ClearDefaultRequestHeaders(
            ILogger logger,
            HttpClient httpClient)
        {
            logger.LogInformation("ClearDefaultRequestHeaders Function called.");

            if (httpClient != null)
                httpClient.DefaultRequestHeaders.Clear();
        }

        /// <summary>
        /// Returns Content Result for final output from the web api
        /// </summary>
        /// <param name="content"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        private static ContentResult GetContentResult(
            string content,
            string contentEncoding,
            int statusCode)
        {
            return new ContentResult
            {
                Content = content,
                ContentType = contentEncoding,
                StatusCode = statusCode
            };
        }
    }
}
