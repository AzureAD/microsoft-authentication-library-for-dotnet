// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class WwwAuthenticateParametersTests
    {
        const string ClientIdKey = "client_id";
        const string ResourceIdKey = "resource_id";
        const string ResourceKey = "resource";
        const string GraphGuid = "00000003-0000-0000-c000-000000000000";
        const string AuthorizationUriKey = "authorization_uri";
        const string AuthorizationKey = "authorization";
        const string AuthorityKey = "authority";
        const string AuthorizationValue = "https://login.microsoftonline.com/common/oauth2/authorize";
        const string Realm = "realm";
        const string EncodedClaims = "eyJpZF90b2tlbiI6eyJhdXRoX3RpbWUiOnsiZXNzZW50aWFsIjp0cnVlfSwiYWNyIjp7InZhbHVlcyI6WyJ1cm46bWFjZTppbmNvbW1vbjppYXA6c2lsdmVyIl19fX0=";
        const string DecodedClaims = "{\"id_token\":{\"auth_time\":{\"essential\":true},\"acr\":{\"values\":[\"urn:mace:incommon:iap:silver\"]}}}";
        const string SomeClaims = "some_claims";

        [TestMethod]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authorization_uri=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authorization=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authority=\"https://login.microsoftonline.com/common/\"")]
        [DataRow("client_id=00000003-0000-0000-c000-000000000000", "authority=\"https://login.microsoftonline.com/common\"")]
        [DataRow("resource_id=00000003-0000-0000-c000-000000000000", "authorization=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        [DataRow("resource=00000003-0000-0000-c000-000000000000", "authorization=\"https://login.microsoftonline.com/common/oauth2/authorize\"")]
        public void CreateWwwAuthenticateResponse(string resource, string authorizationUri)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage((HttpStatusCode)401)
            {
            };
            httpResponse.Headers.Add("WWW-Authenticate", $"Bearer realm=\"\", {resource}, {authorizationUri}");

            var authParams = WwwAuthenticateParameters.ExtractWwwAuthenticateParametersFromHeaders(httpResponse.Headers);

            Assert.AreEqual(GraphGuid, authParams.Resource);
            Assert.AreEqual(TestConstants.AuthorityCommonTenant.TrimEnd('/'), authParams.Authority);
            Assert.AreEqual($"{GraphGuid}/.default", authParams.Scopes.FirstOrDefault());
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
            HttpResponseMessage httpResponse = CreateHttpResponseHeaders(resourceHeaderKey, authorizationUriHeaderKey);

            var authParams = WwwAuthenticateParameters.ExtractWwwAuthenticateParametersFromHeaders(httpResponse.Headers);

            Assert.IsTrue(authParams.RawParameters.ContainsKey(resourceHeaderKey));
            Assert.IsTrue(authParams.RawParameters.ContainsKey(authorizationUriHeaderKey));
            Assert.IsTrue(authParams.RawParameters.ContainsKey(Realm));
        }

        [TestMethod]
        [DataRow(SomeClaims)]
        [DataRow(EncodedClaims)]
        public void CreateRawParameters_ClaimsAndErrorReturned(string claims)
        {
            HttpResponseMessage httpResponse = CreateClaimsHttpResponse(claims);
            var authParams = WwwAuthenticateParameters.ExtractWwwAuthenticateParametersFromHeaders(httpResponse.Headers);

            string claimsKey = "claims";
            string errorKey = "error";
            Assert.IsTrue(authParams.RawParameters.TryGetValue(AuthorizationUriKey, out string authorizationUri));
            Assert.AreEqual(AuthorizationValue, authorizationUri);
            Assert.IsTrue(authParams.RawParameters.ContainsKey(Realm));
            Assert.IsTrue(authParams.RawParameters.TryGetValue(Realm, out string realmValue));
            Assert.AreEqual(string.Empty, realmValue);
            Assert.IsTrue(authParams.RawParameters.TryGetValue(claimsKey, out string claimsValue));
            Assert.AreEqual(claims, claimsValue);
            Assert.IsTrue(authParams.RawParameters.TryGetValue(errorKey, out string errorValue));
            Assert.AreEqual("insufficient_claims", errorValue);
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
            HttpResponseMessage httpResponse = new HttpResponseMessage((HttpStatusCode)401)
            {
            };
            httpResponse.Headers.Add("WWW-Authenticate", $"Bearer realm=\"\", {clientId}, {authorizationUri}");

            var wwwAuthenticateResponse = httpResponse.Headers.WwwAuthenticate.FirstOrDefault().Parameter;

            var authParams = WwwAuthenticateParameters.ExtractParametersFromWwwAuthenticateHeaderValue(wwwAuthenticateResponse);
            
            Assert.AreEqual(GraphGuid, authParams.Resource);
            Assert.AreEqual(TestConstants.AuthorityCommonTenant.TrimEnd('/'), authParams.Authority);
            Assert.AreEqual($"{GraphGuid}/.default", authParams.Scopes.FirstOrDefault());
            Assert.AreEqual(3, authParams.RawParameters.Count);
            Assert.IsNull(authParams.Claims);
            Assert.IsNull(authParams.Error);
        }

        [TestMethod]
        public void ExtractClaimChallengeFromHeader()
        {
            HttpResponseMessage httpResponse = CreateClaimsHttpResponse(SomeClaims);

            string extractedClaims = WwwAuthenticateParameters.ExtractClaimChallengeFromHttpHeader(httpResponse.Headers);

            Assert.AreEqual(SomeClaims, extractedClaims);
        }

        [TestMethod]
        public void ExtractEncodedClaimChallengeFromHeader()
        {
            HttpResponseMessage httpResponse = CreateClaimsHttpResponse(EncodedClaims);

            string extractedClaims = WwwAuthenticateParameters.ExtractClaimChallengeFromHttpHeader(httpResponse.Headers);

            Assert.AreEqual(DecodedClaims, extractedClaims);
        }

        private static HttpResponseMessage CreateClaimsHttpResponse(string claims)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage((HttpStatusCode)401)
            {
            };
            httpResponse.Headers.Add("WWW-Authenticate", $"Bearer realm=\"\", client_id=\"00000003-0000-0000-c000-000000000000\", authorization_uri=\"https://login.microsoftonline.com/common/oauth2/authorize\", error=\"insufficient_claims\", claims=\"{claims}\"");
            return httpResponse;
        }

        private static HttpResponseMessage CreateHttpResponseHeaders(string resourceHeaderKey, string authorizationUriHeaderKey)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage((HttpStatusCode)401)
            {
            };
            httpResponse.Headers.Add("WWW-Authenticate", $"Bearer realm=\"\", {resourceHeaderKey}=\"{GraphGuid}\", {authorizationUriHeaderKey}=\"{AuthorizationValue}\"");
            return httpResponse;
        }
    }
}
