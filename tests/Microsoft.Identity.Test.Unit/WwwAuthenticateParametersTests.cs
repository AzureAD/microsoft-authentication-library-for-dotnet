// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class WwwAuthenticateParametersTests
    {
        private const string WwwAuthenticateHeaderName = "WWW-Authenticate";
        private const string ClientIdKey = "client_id";
        private const string ResourceIdKey = "resource_id";
        private const string ResourceKey = "resource";
        private const string GraphGuid = "00000003-0000-0000-c000-000000000000";
        private const string AuthorizationUriKey = "authorization_uri";
        private const string AuthorizationKey = "authorization";
        private const string AuthorityKey = "authority";
        private const string AuthorizationValue = "https://login.microsoftonline.com/common/oauth2/authorize";
        private const string Realm = "realm";
        private const string EncodedClaims = "eyJpZF90b2tlbiI6eyJhdXRoX3RpbWUiOnsiZXNzZW50aWFsIjp0cnVlfSwiYWNyIjp7InZhbHVlcyI6WyJ1cm46bWFjZTppbmNvbW1vbjppYXA6c2lsdmVyIl19fX0=";
        private const string DecodedClaims = "{\"id_token\":{\"auth_time\":{\"essential\":true},\"acr\":{\"values\":[\"urn:mace:incommon:iap:silver\"]}}}";
        private const string DecodedClaimsHeader = "{\\\"id_token\\\":{\\\"auth_time\\\":{\\\"essential\\\":true},\\\"acr\\\":{\\\"values\\\":[\\\"urn:mace:incommon:iap:silver\\\"]}}}";
        private const string SomeClaims = "some_claims";
        private const string ClaimsKey = "claims";
        private const string ErrorKey = "error";

        [TestMethod]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authorization_uri=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authorization=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authority=\"https://login.microsoftonline.com/common/\"")]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authority=\"https://login.microsoftonline.com/common\"")]
        [DataRow("resource_id=00000003-0000-0000-c000-000000000000", "authorization=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        [DataRow("resource=00000003-0000-0000-c000-000000000000", "authorization=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        public void CreateWwwAuthenticateResponse(string resource, string authorizationUri)
        {
            // Arrange
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.Add(WwwAuthenticateHeaderName, $"Bearer realm=\"\", {resource}, {authorizationUri}");

            // Act
            var authParams = WwwAuthenticateParameters.CreateFromResponseHeaders(httpResponse.Headers);

            // Assert
            Assert.AreEqual(TestConstants.AuthorityCommonTenant.TrimEnd('/'), authParams.Authority);
            Assert.AreEqual(3, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.IsNull(authParams.Error);
        }

        [TestMethod]
        [DataRow(ClientIdKey, AuthorizationUriKey)]
        [DataRow(ClientIdKey, AuthorizationKey)]
        [DataRow(ClientIdKey, AuthorityKey)]
        [DataRow(ResourceIdKey, AuthorizationUriKey)]
        [DataRow(ResourceIdKey, AuthorizationKey)]
        [DataRow(ResourceIdKey, AuthorityKey)]
        [DataRow(ResourceKey, AuthorizationUriKey)]
        [DataRow(ResourceKey, AuthorizationKey)]
        [DataRow(ResourceKey, AuthorityKey)]
        public void CreateRawParameters(string resourceHeaderKey, string authorizationUriHeaderKey)
        {
            // Arrange
            HttpResponseMessage httpResponse = CreateGraphHttpResponse(resourceHeaderKey, authorizationUriHeaderKey);

            // Act
            var authParams = WwwAuthenticateParameters.CreateFromResponseHeaders(httpResponse.Headers);

            // Assert
            Assert.IsTrue(authParams.RawParameters.ContainsKey(resourceHeaderKey));
            Assert.IsTrue(authParams.RawParameters.ContainsKey(authorizationUriHeaderKey));
            Assert.IsTrue(authParams.RawParameters.ContainsKey(Realm));
            Assert.AreEqual(string.Empty, authParams[Realm]);
            Assert.AreEqual(GraphGuid, authParams[resourceHeaderKey]);
            Assert.ThrowsException<KeyNotFoundException>(
                () => authParams[ErrorKey]);
            Assert.ThrowsException<KeyNotFoundException>(
                () => authParams[ClaimsKey]);
        }

        [TestMethod]
        [DataRow(DecodedClaimsHeader)]
        [DataRow(EncodedClaims)]
        [DataRow(SomeClaims)]
        public void CreateRawParameters_ClaimsAndErrorReturned(string claims)
        {
            // Arrange
            HttpResponseMessage httpResponse = CreateClaimsHttpResponse(claims);

            // Act
            var authParams = WwwAuthenticateParameters.CreateFromResponseHeaders(httpResponse.Headers);

            // Assert
            const string errorValue = "insufficient_claims";
            Assert.IsTrue(authParams.RawParameters.TryGetValue(AuthorizationUriKey, out string authorizationUri));
            Assert.AreEqual(AuthorizationValue, authorizationUri);
            Assert.AreEqual(AuthorizationValue, authParams[AuthorizationUriKey]);
            Assert.IsTrue(authParams.RawParameters.ContainsKey(Realm));
            Assert.IsTrue(authParams.RawParameters.TryGetValue(Realm, out string realmValue));
            Assert.AreEqual(string.Empty, realmValue);
            Assert.AreEqual(string.Empty, authParams[Realm]);
            Assert.IsTrue(authParams.RawParameters.TryGetValue(ClaimsKey, out string claimsValue));
            Assert.AreEqual(claims, claimsValue);
            Assert.AreEqual(claimsValue, authParams[ClaimsKey]);
            Assert.IsTrue(authParams.RawParameters.TryGetValue(ErrorKey, out string errorValueParam));
            Assert.AreEqual(errorValue, errorValueParam);
            Assert.AreEqual(errorValue, authParams[ErrorKey]);
        }

        [TestMethod]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authorization_uri=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authorization=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authority=\"https://login.microsoftonline.com/common/\"")]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authority=\"https://login.microsoftonline.com/common\"")]
        [DataRow("resource_id=00000003-0000-0000-c000-000000000000", "authorization=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        [DataRow("resource=00000003-0000-0000-c000-000000000000", "authorization=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        public void CreateWwwAuthenticateParamsFromWwwAuthenticateHeader(string clientId, string authorizationUri)
        {
            // Arrange
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.Add(WwwAuthenticateHeaderName, $"Bearer {clientId}, {authorizationUri}");
            httpResponse.Headers.Add(WwwAuthenticateHeaderName, $"Bearer realm=\"\", {clientId}, {authorizationUri}");
            httpResponse.Headers.Add(WwwAuthenticateHeaderName, $"Bearer realm=\"\" token68 {clientId}, {authorizationUri}");

            var wwwAuthenticateResponse1 = httpResponse.Headers.WwwAuthenticate.First().Parameter;
            var wwwAuthenticateResponse2 = httpResponse.Headers.WwwAuthenticate.ToList()[1].Parameter;
            var wwwAuthenticateResponse3 = httpResponse.Headers.WwwAuthenticate.Last().Parameter;

            // Act
            var authParams = WwwAuthenticateParameters.CreateFromWwwAuthenticateHeaderValue(wwwAuthenticateResponse1);
            var authParams2 = WwwAuthenticateParameters.CreateFromWwwAuthenticateHeaderValue(wwwAuthenticateResponse2);
            var authParams3 = WwwAuthenticateParameters.CreateFromWwwAuthenticateHeaderValue(wwwAuthenticateResponse3);

            // Assert
            Assert.AreEqual(TestConstants.AuthorityCommonTenant.TrimEnd('/'), authParams.Authority);
            Assert.AreEqual(3, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.IsNull(authParams.Error);

            Assert.AreEqual(TestConstants.AuthorityCommonTenant.TrimEnd('/'), authParams2.Authority);
            Assert.AreEqual(3, authParams2.RawParameters.Count);
            Assert.IsNull(authParams2.Claims);
            Assert.IsNull(authParams2.Error);

            Assert.AreEqual(TestConstants.AuthorityCommonTenant.TrimEnd('/'), authParams3.Authority);
            Assert.AreEqual(3, authParams3.RawParameters.Count);
            Assert.IsNull(authParams3.Claims);
            Assert.IsNull(authParams3.Error);
        }

        [TestMethod]
        public async Task CreateFromResourceResponseAsync_HttpClient_Arm_GetTenantId_Async()
        {
            const string resourceUri = "https://example.com/";
            string tenantId = Guid.NewGuid().ToString();

            var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ExpectedUrl = resourceUri,
                ResponseMessage = CreateInvalidTokenHttpErrorResponse(tenantId)
            };
            var httpClient = new HttpClient(handler);
            var authParams = await WwwAuthenticateParameters.CreateFromResourceResponseAsync(httpClient, resourceUri).ConfigureAwait(false);

            Assert.AreEqual(authParams.GetTenantId(), tenantId);

            var authParamList = await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(httpClient, resourceUri).ConfigureAwait(false);

            Assert.AreEqual(authParamList.FirstOrDefault().GetTenantId(), tenantId);
        }

        [TestMethod]
        public async Task CreateFromResourceResponseAsync_HttpClient_B2C_GetTenantId_Async()
        {
            const string resourceUri = "https://example.com/";
            const string tenantId = "tenant";

            var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ExpectedUrl = resourceUri,
                ResponseMessage = CreateInvalidTokenHttpErrorResponse(authority: TestConstants.B2CAuthority)
            };
            var httpClient = new HttpClient(handler);
            var authParams = await WwwAuthenticateParameters.CreateFromResourceResponseAsync(httpClient, resourceUri).ConfigureAwait(false);

            Assert.AreEqual(authParams.GetTenantId(), tenantId);

            var authParamList = await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(httpClient, resourceUri).ConfigureAwait(false);

            Assert.AreEqual(authParamList.FirstOrDefault().GetTenantId(), tenantId);
        }

        [TestMethod]
        [DataRow(TestConstants.ADFSAuthority)]
        [DataRow(TestConstants.ADFSAuthority2)]
        public async Task CreateFromResourceResponseAsync_HttpClient_ADFS_GetTenantId_Null_Async(string authority)
        {
            const string resourceUri = "https://example.com/";
            string tenantId = Guid.NewGuid().ToString();

            var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ExpectedUrl = resourceUri,
                ResponseMessage = CreateInvalidTokenHttpErrorResponse(tenantId, authority)
            };
            var httpClient = new HttpClient(handler);
            var authParams = await WwwAuthenticateParameters.CreateFromResourceResponseAsync(httpClient, resourceUri).ConfigureAwait(false);

            Assert.IsNull(authParams.GetTenantId());

            var authParamList = await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(httpClient, resourceUri).ConfigureAwait(false);

            Assert.IsNull(authParamList.FirstOrDefault().GetTenantId());
        }

        [DataRow(null)]
        [TestMethod]
        public async Task CreateFromResourceResponseAsync_HttpClient_Null_Async(HttpClient httpClient)
        {
            const string resourceUri = "https://example.com/";

            Func<Task> action = () => WwwAuthenticateParameters.CreateFromResourceResponseAsync(httpClient, resourceUri);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(action).ConfigureAwait(false);

            Func<Task> action2 = () => WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(httpClient, resourceUri);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(action2).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public async Task CreateFromResourceResponseAsync_Incorrect_ResourceUri_Async(string resourceUri)
        {

            await WwwAuthenticateParameters.CreateFromResourceResponseAsync("https://manage.office.com/api/v1.0/fbb86d84-7975-4300-a5cb-87b448d6f13d/activity/feed/subscriptions/content?contentType={ContentType}&amp;startTime={0}&amp;endTime={1}").ConfigureAwait(false);

            Func<Task> action = () => WwwAuthenticateParameters.CreateFromResourceResponseAsync(resourceUri);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(action).ConfigureAwait(false);
        }

        [TestMethod]
        public void ExtractClaimChallengeFromHeader()
        {
            // Arrange
            HttpResponseMessage httpResponse = CreateClaimsHttpResponse(DecodedClaimsHeader);

            // Act
            string extractedClaims = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(httpResponse.Headers);

            // Assert
            Assert.AreEqual(DecodedClaimsHeader, extractedClaims);
        }

        [TestMethod]
        public void ExtractEncodedClaimChallengeFromHeader()
        {
            // Arrange
            HttpResponseMessage httpResponse = CreateClaimsHttpResponse(EncodedClaims);

            // Act
            string extractedClaims = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(httpResponse.Headers);

            // Assert
            Assert.AreEqual(DecodedClaims, extractedClaims);
        }

        [TestMethod]
        public void ExtractClaimChallengeFromHeader_IncorrectError_ReturnNull()
        {
            // Arrange
            HttpResponseMessage httpResponse = CreateClaimsHttpErrorResponse();

            // Act & Assert
            Assert.IsNull(WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(httpResponse.Headers));
        }

        [TestMethod]
        public async Task ExtractNonceFromHeaderAsync()
        {
            //Arrange & Act
            var parameterList = await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(
                                                         "https://testingsts.azurewebsites.net/servernonce/invalidsignature").ConfigureAwait(false);

            //Assert
            Assert.IsTrue(parameterList.FirstOrDefault().AuthScheme == Constants.PoPAuthHeaderPrefix);
            Assert.IsNotNull(parameterList.FirstOrDefault().ServerNonce);
        }

        [TestMethod]
        public async Task CreateAllFromResourceResponseAsync_HttpClient_Bearer_Pop_Async()
        {
            const string resourceUri = "https://example.com/";
            string tenantId = Guid.NewGuid().ToString();

            var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ExpectedUrl = resourceUri,
                ResponseMessage = CreateBearerAndPopHttpResponse()
            };

            var httpClient = new HttpClient(handler);
            var headers = await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(httpClient, resourceUri).ConfigureAwait(false);

            var bearerHeader = headers.Where(header => header.AuthScheme == "Bearer").FirstOrDefault();
            var popHeader = headers.Where(header => header.AuthScheme == "PoP").FirstOrDefault();

            Assert.IsNotNull(bearerHeader);
            Assert.AreEqual("https://login.microsoftonline.com/TenantId", bearerHeader.Authority);
            Assert.IsNotNull(popHeader);
            Assert.AreEqual("someNonce", popHeader.ServerNonce);
        }

        [TestMethod]
        public void ExtractAllParametersFromResponse()
        {
            // Arrange
            HttpResponseMessage httpResponse = CreateBearerAndPopHttpResponse();

            // Act & Assert
            var headers = WwwAuthenticateParameters.CreateFromAuthenticateHeaders(httpResponse.Headers);
            var bearerHeader = headers.Where(header => header.AuthScheme == "Bearer").FirstOrDefault();
            var popHeader = headers.Where(header => header.AuthScheme == "PoP").FirstOrDefault();

            Assert.IsNotNull(bearerHeader);
            Assert.AreEqual("https://login.microsoftonline.com/TenantId", bearerHeader.Authority);
            Assert.IsNotNull(popHeader);
            Assert.AreEqual("someNonce", popHeader.ServerNonce);
        }

        [TestMethod]
        //Test for fix https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3026
        public void ExtractParametersFromResponseWithScheme()
        {
            //arrange
            var header = WwwAuthenticateParameters.CreateFromWwwAuthenticateHeaderValue("Bearer authorization_uri=https://login.microsoftonline.com/TenantId/oauth2/authorize, resource_id=https://endpoint/");

            // Act & Assert
            Assert.IsNotNull(header);
            Assert.AreEqual("https://login.microsoftonline.com/TenantId", header.Authority);
        }

        private static HttpResponseMessage CreateClaimsHttpResponse(string claims)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.Add(WwwAuthenticateHeaderName, $"Bearer realm=\"\", client_id=\"00000003-0000-0000-c000-000000000000\", authorization_uri=\"https://login.microsoftonline.com/common/oauth2/authorize\", error=\"insufficient_claims\", claims=\"{claims}\"");
            return httpResponse;
        }

        private static HttpResponseMessage CreateClaimsHttpErrorResponse()
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers =
                {
                    { WwwAuthenticateHeaderName, $"Bearer realm=\"\", client_id=\"00000003-0000-0000-c000-000000000000\", authorization_uri=\"https://login.microsoftonline.com/common/oauth2/authorize\", error=\"some_error\", claims=\"{DecodedClaimsHeader}\"" }
                }
            };
        }

        private static HttpResponseMessage CreateGraphHttpResponse(string resourceHeaderKey, string authorizationUriHeaderKey)
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers =
                {
                    { WwwAuthenticateHeaderName, $"Bearer realm=\"\", {resourceHeaderKey}=\"{GraphGuid}\", {authorizationUriHeaderKey}=\"{AuthorizationValue}\"" }
                }
            };
        }

        private static HttpResponseMessage CreateInvalidTokenHttpErrorResponse(string tenantId = "", string authority = "https://login.windows.net")
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers =
                {
                    { WwwAuthenticateHeaderName, $"Bearer authorization_uri=\"{authority}/{tenantId}\", error=\"invalid_token\", error_description=\"The authentication failed because of missing 'Authorization' header.\"" }
                }
            };
        }

        private static HttpResponseMessage CreateBearerAndPopHttpResponse()
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers =
                {
                    { WwwAuthenticateHeaderName, $"Bearer authorization_uri=\"https://login.microsoftonline.com/TenantId/oauth2/authorize\", resource_id=\"https://endpoint/\"" },
                    { WwwAuthenticateHeaderName, $"PoP nonce=\"someNonce\""}
                }
            };
        }
    }
}
