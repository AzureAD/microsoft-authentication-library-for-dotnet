// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using System;
using System.Security.Claims;
using System.Net.Http;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Client.Internal;
using System.Net.Http.Headers;
using System.Net;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public class PoPValidator
    {
        // This endpoint is hosted in the MSID Lab and is able to verify any pop token bound to an HTTP request
        private const string PoPValidatorEndpoint = "https://signedhttprequest.azurewebsites.net/api/validateSHR";
        private static HttpClient s_httpClient = new HttpClient();
        private static Lazy<string> s_popValidationEndpointLazy = new Lazy<string>(
            () => LabUserHelper.KeyVaultSecretsProviderMsal.GetSecretByName(
                "automation-pop-validation-endpoint",
                "841fc7c2ccdd48d7a9ef727e4ae84325").Value);

        /// <summary>
        /// This calls a special endpoint that validates any POP token against a configurable HTTP request.
        /// The HTTP request is configured through headers.
        /// </summary>
        public static void VerifyPoPToken(
            string clientId, 
            string requestUri, 
            HttpMethod method, 
            AuthenticationResult result)
        {
            VerifyPoPToken(clientId, requestUri, method, result.AccessToken, result.TokenType);
        }

        public static void VerifyPoPToken(
            string clientId, 
            string requestUri, 
            HttpMethod method, 
            string token, 
            string tokenType)
        {
            Uri protectedUri = new Uri(requestUri);

            ClaimsPrincipal popClaims = IdToken.Parse(token).ClaimsPrincipal;
            string assertionWithoutShr = popClaims.FindFirst("at").Value;
            string shrM = popClaims.FindFirst("m").Value;
            Assert.AreEqual(method.ToString(), shrM, "Method mismatch");
            string shrU = popClaims.FindFirst("u").Value;
            Assert.AreEqual(protectedUri.Host, shrU, "Host mismatch");
            string shrP = popClaims.FindFirst("p").Value;
            Assert.AreEqual(protectedUri.LocalPath, shrP, "Path mismatch");
            string ts = popClaims.FindFirst("ts").Value;
            Assert.IsTrue(int.TryParse(ts, out int _), "timestamp");
            string cnf = popClaims.FindFirst("cnf").Value;
            Assert.IsNotNull(cnf);
            ClaimsPrincipal innerTokenClaims = IdToken.Parse(assertionWithoutShr).ClaimsPrincipal;
            string reqCnf = innerTokenClaims.FindFirst("cnf").Value;
            Assert.IsNotNull(reqCnf);
        }

        public static async Task VerifyPopNonceAsync(string nonce)
        {
            var response = await s_httpClient.GetAsync(
                $"https://testingsts.azurewebsites.net/servernonce/validate?serverNonce={nonce}").ConfigureAwait(false);

            Assert.AreEqual(response.StatusCode, System.Net.HttpStatusCode.OK);
        }

    }
}
