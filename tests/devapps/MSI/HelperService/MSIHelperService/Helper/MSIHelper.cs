// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Web;

namespace MSIHelperService.Helper
{
    internal static class MSIHelper
    {
        //content type
        internal const string ContentTypeJson = "application/json";
        internal const string ContentTypeTextOrHtml = "text/html";
        internal const string ContentTypeMulipartOrFormData = "multipart/form-data";
        internal const string DefaultAzureResource = "webapp";
        internal const string ManagedIdentityAuthenticationHeader = "X-IDENTITY-HEADER";

        //Environment variables
        internal static readonly string? s_requestAppID = Environment.GetEnvironmentVariable("requestAppID");
        internal static readonly string? s_requestAppSecret = Environment.GetEnvironmentVariable("requestAppSecret");
        internal static readonly string? s_functionAppUri = Environment.GetEnvironmentVariable("functionAppUri");
        internal static readonly string? s_functionAppEnvCode = Environment.GetEnvironmentVariable("functionAppEnvCode");
        internal static readonly string? s_functionAppMSICode = Environment.GetEnvironmentVariable("functionAppMSICode");
        internal static readonly string? s_webhookLocation = Environment.GetEnvironmentVariable("webhookLocation");
        internal static readonly string? s_oMSAdminClientID = Environment.GetEnvironmentVariable("OMSAdminClientID");
        internal static readonly string? s_oMSAdminClientSecret = Environment.GetEnvironmentVariable("OMSAdminClientSecret");
        internal const string Authority = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47";

        //Enum for HTTP Error Response Codes
        internal enum HTTPErrorResponseCode : int
        {
            Status200OK = StatusCodes.Status200OK,
            Status201Created = StatusCodes.Status201Created,
            Status400BadRequest = StatusCodes.Status400BadRequest,
            Status404NotFound = StatusCodes.Status404NotFound,
            Status500InternalServerError = StatusCodes.Status500InternalServerError,
            Status503ServiceUnavailable = StatusCodes.Status503ServiceUnavailable
        }

        //Azure Resources
        internal enum AzureResource
        {
            webapp,
            function,
            vm,
            azurearc,
            servicefabric,
            cloudshell
        }

        /// <summary>
        /// Gets the Environment Variables from the Azure Web App
        /// </summary>
        /// <param name="logger"></param>
        /// <returns>Returns the environment variables</returns>
        public static Dictionary<string, string> GetWebAppEnvironmentVariables(
            ILogger logger)
        {
            //Gets Azure Web App Specific environment variables and sends it back
            //Sending back the specific ones that is needed for the MSI tests
            Dictionary<string, string>? keyValuePairs = new();
            keyValuePairs.Add("IDENTITY_HEADER", Environment.GetEnvironmentVariable("IDENTITY_HEADER") ?? "");
            keyValuePairs.Add("IDENTITY_ENDPOINT", Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT") ?? "");
            keyValuePairs.Add("IDENTITY_API_VERSION", Environment.GetEnvironmentVariable("IDENTITY_API_VERSION") ?? "");

            logger.LogInformation("GetWebAppEnvironmentVariables Function called.");

            return keyValuePairs;
        }

        /// <summary>
        /// Gets the Environment Variable from the Azure Function
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <returns>Returns the environment variables</returns>
        public static Dictionary<string, string>? GetFunctionAppEnvironmentVariables(
            HttpClient httpClient,
            ILogger logger)
        {
            logger.LogInformation("GetFunctionAppEnvironmentVariables Function called.");

            string? token = Task.Run(async () => await GetMSALToken(logger).ConfigureAwait(false))
                .GetAwaiter().GetResult();

            //clear the default request header for each call
            ClearDefaultRequestHeaders(logger, httpClient);

            //Set the Authorization header
            SetAuthorizationHeader(token, httpClient, logger);

            //send the request
            HttpResponseMessage result = Task.Run(async () => await httpClient
            .GetAsync(s_functionAppUri + "GetEnvironmentVariables?code=" + s_functionAppEnvCode).ConfigureAwait(false))
            .GetAwaiter().GetResult();

            var content = Task.Run(
                async () => await result.Content.ReadAsStringAsync().ConfigureAwait(false))
                .GetAwaiter().GetResult();

            Dictionary<string, string>? envValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

            logger.LogInformation("GetFunctionAppEnvironmentVariables call was successful.");

            return envValuePairs;
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
            HttpResponseMessage? result = await httpClient.SendAsync(requestMessage)
                .ConfigureAwait(false);

            string body = await result.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            logger.LogInformation("GetWebAppMSIToken Function call was successful.");

            return GetContentResult(body, "application/json", (int)result.StatusCode);
        }

        /// <summary>
        /// Gets the MSI Token from the Azure Function App
        /// </summary>
        /// <param name="identityHeader"></param>
        /// <param name="uri"></param>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <returns>Returns MSI Token</returns>
        public static async Task<ActionResult?> GetFunctionAppMSIToken(
            string identityHeader,
            string uri,
            HttpClient httpClient,
            ILogger logger)
        {
            logger.LogInformation("GetFunctionAppMSIToken Function called.");

            string? token = await GetMSALToken(logger)
                .ConfigureAwait(false);

            //clear the default request header for each call
            ClearDefaultRequestHeaders(logger, httpClient);

            //Set the Authorization header
            SetAuthorizationHeader(token, httpClient, logger);

            //send the request
            var encodedUri = HttpUtility.UrlEncode(uri);

            HttpResponseMessage result = await httpClient.GetAsync(s_functionAppUri + "getToken?code=" +
                s_functionAppMSICode + "&uri=" + encodedUri + "&header=" +identityHeader)
                .ConfigureAwait(false);

            string body = await result.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            logger.LogInformation("GetFunctionAppMSIToken call was successful.");

            return GetContentResult(body, "application/json", (int)result.StatusCode);
        }

        /// <summary>
        /// Get the Client Token 
        /// </summary>
        /// <param name="appID"></param>
        /// <param name="appSecret"></param>
        /// <param name="scopes"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task<string?> GetMSALToken(ILogger logger)
        {
            //Scopes
            string[] scopes = new string[] { "https://request.msidlab.com/.default" };

            logger.LogInformation("GetMSALToken Function called.");

            //Confidential Client Application Builder
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(s_requestAppID)
           .WithClientSecret(s_requestAppSecret)
           .WithAuthority(new Uri(Authority))
           .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
           .Build();

            //Acquire Token For Client using MSAL
            try
            {
                AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync().ConfigureAwait(false);
                logger.LogInformation("MSAL Token acquired successfully.");
                logger.LogInformation($"MSAL Token source is : { result.AuthenticationResultMetadata.TokenSource }");
                return result.AccessToken;
            }
            catch (MsalException ex)
            {
                logger.LogError(ex.Message);
                return ex.Message;
            }
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
                httpClient?.DefaultRequestHeaders.Clear();
        }

        /// <summary>
        /// Sets the authorization header on the http client
        /// </summary>
        /// <param name="token"></param>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static void SetAuthorizationHeader(
            string? token,
            HttpClient httpClient,
            ILogger logger)
        {
            logger.LogInformation("SetAuthorizationHeader Function called.");
            
            if(httpClient!=null)
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Gets a JSON String using Newtonsoft JSON Convert
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string ConvertToJsonString(object? content)
        {
            string json;

            json = JsonConvert.SerializeObject
                (content, Formatting.Indented, new JsonSerializerSettings()
                { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, TypeNameHandling = TypeNameHandling.None });

            return json.Trim();

        }

        /// <summary>
        /// Returns Content Result for final output from the web api
        /// </summary>
        /// <param name="content"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static ContentResult GetContentResult(string content, string contentEncoding, int statusCode)
        {
            if (statusCode == (int)MSIHelper.HTTPErrorResponseCode.Status200OK)
            { 
                ManagedIdentityResponse? managedIdentityResponse = JsonConvert.DeserializeObject<ManagedIdentityResponse>(content);

                //return ManagedIdentityResponse object so we can trim the access token
                return new ContentResult { 
                    Content = ConvertToJsonString(managedIdentityResponse), 
                    ContentType = contentEncoding, 
                    StatusCode = statusCode 
                };
            }

            //return errors as is from the MSI Endpoints
            return new ContentResult { 
                Content = ConvertToJsonString(content),
                ContentType = contentEncoding, 
                StatusCode = statusCode 
            };
        }
    }
}
