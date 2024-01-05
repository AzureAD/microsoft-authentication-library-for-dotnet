// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    // This is meant to stay in sync with iOS code in https://github.com/AzureAD/microsoft-authentication-library-common-for-objc/blob/master/IdentityCore/tests/integration/MSIDCacheSchemaValidationTests.m
    // This is for tests laid out here:  https://identitydivision.visualstudio.com/DevEx/_git/AuthLibrariesApiReview?path=%2FUnifiedSchema%2Ftestcases&version=GBdev

    [TestClass]
    [TestCategory(TestCategories.UnifiedSchemaValidation)]
    public class UnifiedSchemaValidationTests : TestBase
    {
        private const string ClientId = "b6c69a37-df96-4db0-9088-2ab96e1d8215";
        private const string B2CClientId = "0a7f52dd-260e-432f-94de-b47828c3f372";
        private const string B2CScopes = "https://iosmsalb2c.onmicrosoft.com/webapitest/user.read";
        private const string AuthorityUri = "https://login.microsoftonline.com/common";
        private const string MsalEnvironment = "login.microsoftonline.com";
        private const string AadTenantId = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
        private const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
        private const string B2CTenantId = "ba6c0d94-a8da-45b2-83ae-33871f9c2dd8";
        private const string Scopes = "tasks.read user.read openid profile offline_access";
        private const string RedirectUri = "msalb6c69a37-df96-4db0-9088-2ab96e1d8215://auth";

        // Our json schemas are flat.  This will NOT work for deeply nested json values.  You could check Assert.IsTrue(JToken.DeepEquals()) if you need that.
        private void AssertAreJsonStringsEquivalent(string expectedJson, string actualJson)
        {
            var expectedObj = JObject.Parse(expectedJson);
            var actualObj = JObject.Parse(actualJson);

            var expectedDict = expectedObj.ToObject<Dictionary<string, object>>();
            var actualDict = actualObj.ToObject<Dictionary<string, object>>();

            string message = $"{Environment.NewLine}{Environment.NewLine}Json Expected <{expectedJson}> {Environment.NewLine}{Environment.NewLine}Json Actual <{actualJson}>";

            foreach (var kvp in expectedDict)
            {
                Assert.IsTrue(actualDict.ContainsKey(kvp.Key), $"actualJson does not contain key: {kvp.Key}. {message}");
                Assert.AreEqual(kvp.Value, actualDict[kvp.Key], $"actualJson has different value for ({kvp.Key}).  Expected: <{kvp.Value}>  Actual: <{actualDict[kvp.Key]}> {message}");
            }

            foreach (var kvp in actualDict)
            {
                Assert.IsTrue(expectedDict.ContainsKey(kvp.Key), $"actualJson has unexpected key: {kvp.Key} {message}");
                Assert.AreEqual(kvp.Value, expectedDict[kvp.Key], $"actualJson has different value for ({kvp.Key}).  Expected: <{expectedDict[kvp.Key]}>  Actual: <{kvp.Key}> {message}");
            }
        }

        [TestMethod]
        public void TestSchemaComplianceForAccessToken_WhenMSSTSResponse_WithAADAccount()
        {
            string homeAccountId = ClientInfo.CreateFromJson(TestConstants.AadRawClientInfo).ToAccountIdentifier();
            var credential = new MsalAccessTokenCacheItem(
                MsalEnvironment,
                ClientId,
                TestConstants.CreateAadTestTokenResponse(),
                AadTenantId,
                homeAccountId);

            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload
            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_at>",
                ["target"] = "Calendars.Read email openid profile Tasks.Read User.Read",
                ["extended_expires_on"] = extExpiresOn,
                ["ext_expires_on"] = extExpiresOn,
                ["credential_type"] = "AccessToken",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["expires_on"] = expiresOn,
                ["cached_at"] = cachedAt,
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["home_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["client_info"] = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            // 2. Verify cache key
            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "accesstoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-calendars.read email openid profile tasks.read user.read";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "accesstoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2001, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForIDToken_WhenMSSTSResponse_WithAADAccount()
        {
            string homeAccountId = ClientInfo.CreateFromJson(TestConstants.AadRawClientInfo).ToAccountIdentifier();

            var credential = new MsalIdTokenCacheItem(
                MsalEnvironment,
                ClientId,
                TestConstants.CreateAadTestTokenResponse(),
                AadTenantId,
                homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJiNmM2OWEzNy1kZjk2LTRkYjAtOTA4OC0yYWI5NmUxZDgyMTUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhL3YyLjAiLCJpYXQiOjE1Mzg1Mzg0MjIsIm5iZiI6MTUzODUzODQyMiwiZXhwIjoxNTM4NTQyMzIyLCJuYW1lIjoiQ2xvdWQgSURMQUIgQmFzaWMgVXNlciIsIm9pZCI6IjlmNDg4MGQ4LTgwYmEtNGM0MC05N2JjLWY3YTIzYzcwMzA4NCIsInByZWZlcnJlZF91c2VybmFtZSI6ImlkbGFiQG1zaWRsYWI0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6Ilk2WWtCZEhOTkxITm1US2VsOUtoUno4d3Jhc3hkTFJGaVAxNEJSUFdybjQiLCJ0aWQiOiJmNjQ1YWQ5Mi1lMzhkLTRkMWEtYjUxMC1kMWIwOWE3NGE4Y2EiLCJ1dGkiOiI2bmNpWDAyU01raTlrNzMtRjFzWkFBIiwidmVyIjoiMi4wIn0.",
                ["credential_type"] = "IdToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["realm"] = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["client_info"] = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "idtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "idtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForRefreshToken_WhenMSSTSResponse_WithAADAccount()
        {
            var response = TestConstants.CreateAadTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalRefreshTokenCacheItem(
                MsalEnvironment,
                ClientId,
                response,
                homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_rt>",
                ["credential_type"] = "RefreshToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["client_info"] = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "refreshtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215--";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "refreshtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2002, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccount_WhenMSSTSResponse_WithAADAccount()
        {
            var response = TestConstants.CreateAadTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();
            var idToken = IdToken.Parse(response.IdToken);

            var credential = new MsalAccountCacheItem(
                MsalEnvironment,
                response.ClientInfo,
                homeAccountId,
                idToken,
                "idlab@msidlab4.onmicrosoft.com",
                AadTenantId,
                null);

            var expectedJsonObject = new JObject
            {
                ["local_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084",
                ["home_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["username"] = "idlab@msidlab4.onmicrosoft.com",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["authority_type"] = "MSSTS",
                ["name"] = "Cloud IDLAB Basic User",
                ["client_info"] = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "idlab@msidlab4.onmicrosoft.com";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(1003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccessToken_WhenMSSTSResponse_WithMSAAccount()
        {
            var response = TestConstants.CreateMsaTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalAccessTokenCacheItem(
                MsalEnvironment,
                ClientId,
                response,
                MsaTenantId,
                homeAccountId);

            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload
            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_at>",
                ["target"] = "openid profile Tasks.Read User.Read",
                ["extended_expires_on"] = extExpiresOn,
                ["ext_expires_on"] = extExpiresOn,
                ["credential_type"] = "AccessToken",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "9188040d-6c67-4c5b-b112-36a304b66dad",
                ["expires_on"] = expiresOn,
                ["cached_at"] = cachedAt,
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["home_account_id"] = "00000000-0000-0000-40c0-3bac188d01d1.9188040d-6c67-4c5b-b112-36a304b66dad",
                ["client_info"] = "eyJ2ZXIiOiIxLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwibmFtZSI6Ik9sZ2EgRGFsdG9tIiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20iLCJvaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtNDBjMC0zYmFjMTg4ZDAxZDEiLCJ0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQiLCJob21lX29pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInVpZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInV0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQifQ"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            // 2. Verify cache key
            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "accesstoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-9188040d-6c67-4c5b-b112-36a304b66dad-openid profile tasks.read user.read";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "00000000-0000-0000-40c0-3bac188d01d1.9188040d-6c67-4c5b-b112-36a304b66dad-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "accesstoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-9188040d-6c67-4c5b-b112-36a304b66dad";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2001, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForIDToken_WhenMSSTSResponse_WithMSAAccount()
        {
            var response = TestConstants.CreateMsaTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalIdTokenCacheItem(
                MsalEnvironment,
                ClientId,
                response,
                MsaTenantId,
                homeAccountId: homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "eyJ2ZXIiOiIyLjAiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vOTE4ODA0MGQtNmM2Ny00YzViLWIxMTItMzZhMzA0YjY2ZGFkL3YyLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwiYXVkIjoiYjZjNjlhMzctZGY5Ni00ZGIwLTkwODgtMmFiOTZlMWQ4MjE1IiwiZXhwIjoxNTM4ODg1MjU0LCJpYXQiOjE1Mzg3OTg1NTQsIm5iZiI6MTUzODc5ODU1NCwibmFtZSI6IlRlc3QgVXNlcm5hbWUiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJtc2Fsc2RrdGVzdEBvdXRsb29rLmNvbSIsIm9pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInRpZCI6IjkxODgwNDBkLTZjNjctNGM1Yi1iMTEyLTM2YTMwNGI2NmRhZCIsImFpbyI6IkRXZ0tubCFFc2ZWa1NVOGpGVmJ4TTZQaFphUjJFeVhzTUJ5bVJHU1h2UkV1NGkqRm1CVTFSQmw1aEh2TnZvR1NHbHFkQkpGeG5kQXNBNipaM3FaQnIwYzl2YUlSd1VwZUlDVipTWFpqdzghQiIsImFsZyI6IkhTMjU2In0.",
                ["credential_type"] = "IdToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "00000000-0000-0000-40c0-3bac188d01d1.9188040d-6c67-4c5b-b112-36a304b66dad",
                ["realm"] = "9188040d-6c67-4c5b-b112-36a304b66dad",
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["client_info"] = "eyJ2ZXIiOiIxLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwibmFtZSI6Ik9sZ2EgRGFsdG9tIiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20iLCJvaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtNDBjMC0zYmFjMTg4ZDAxZDEiLCJ0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQiLCJob21lX29pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInVpZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInV0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQifQ"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "idtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-9188040d-6c67-4c5b-b112-36a304b66dad-";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "00000000-0000-0000-40c0-3bac188d01d1.9188040d-6c67-4c5b-b112-36a304b66dad-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "idtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-9188040d-6c67-4c5b-b112-36a304b66dad";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForRefreshToken_WhenMSSTSResponse_WithMSAAccount()
        {
            var response = TestConstants.CreateMsaTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalRefreshTokenCacheItem(
                MsalEnvironment,
                ClientId,
                response,
                homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_rt>",
                ["credential_type"] = "RefreshToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "00000000-0000-0000-40c0-3bac188d01d1.9188040d-6c67-4c5b-b112-36a304b66dad",
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["client_info"] = "eyJ2ZXIiOiIxLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwibmFtZSI6Ik9sZ2EgRGFsdG9tIiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20iLCJvaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtNDBjMC0zYmFjMTg4ZDAxZDEiLCJ0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQiLCJob21lX29pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInVpZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInV0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQifQ"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "refreshtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215--";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "00000000-0000-0000-40c0-3bac188d01d1.9188040d-6c67-4c5b-b112-36a304b66dad-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "refreshtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2002, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccessToken_WhenMSSTSResponse_WithB2CAccount()
        {
            var response = TestConstants.CreateB2CTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalAccessTokenCacheItem(
                MsalEnvironment,
                B2CClientId,
                TestConstants.CreateB2CTestTokenResponse(),
                B2CTenantId,
                homeAccountId);

            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload
            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_at>",
                ["target"] = "https://iosmsalb2c.onmicrosoft.com/webapitest/user.read",
                ["extended_expires_on"] = extExpiresOn,
                ["ext_expires_on"] = extExpiresOn,
                ["credential_type"] = "AccessToken",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["expires_on"] = expiresOn,
                ["cached_at"] = cachedAt,
                ["client_id"] = "0a7f52dd-260e-432f-94de-b47828c3f372",
                ["home_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["client_info"] = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            // 2. Verify cache key
            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "accesstoken-0a7f52dd-260e-432f-94de-b47828c3f372-ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-https://iosmsalb2c.onmicrosoft.com/webapitest/user.read";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "accesstoken-0a7f52dd-260e-432f-94de-b47828c3f372-ba6c0d94-a8da-45b2-83ae-33871f9c2dd8";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2001, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForIDToken_WhenMSSTSResponse_WithB2CAccount()
        {
            var response = TestConstants.CreateB2CTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalIdTokenCacheItem(
                MsalEnvironment,
                B2CClientId,
                response,
                B2CTenantId,
                homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1Mzg4MDQ4NjAsIm5iZiI6MTUzODgwMTI2MCwidmVyIjoiMS4wIiwiaXNzIjoiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tL2JhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOC92Mi4wLyIsInN1YiI6ImFkMDIwZjhlLWIxYmEtNDRiMi1iZDY5LWMyMmJlODY3MzdmNSIsImF1ZCI6IjBhN2Y1MmRkLTI2MGUtNDMyZi05NGRlLWI0NzgyOGMzZjM3MiIsImlhdCI6MTUzODgwMTI2MCwiYXV0aF90aW1lIjoxNTM4ODAxMjYwLCJpZHAiOiJsaXZlLmNvbSIsIm5hbWUiOiJNU0FMIFNESyBUZXN0Iiwib2lkIjoiYWQwMjBmOGUtYjFiYS00NGIyLWJkNjktYzIyYmU4NjczN2Y1IiwiZmFtaWx5X25hbWUiOiJTREsgVGVzdCIsImdpdmVuX25hbWUiOiJNU0FMIiwiZW1haWxzIjpbIm1zYWxzZGt0ZXN0QG91dGxvb2suY29tIl0sInRmcCI6IkIyQ18xX1NpZ25pbiIsImF0X2hhc2giOiJRNE8zSERDbGNhTGw3eTB1VS1iSkFnIn0.",
                ["credential_type"] = "IdToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["realm"] = "ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["client_id"] = "0a7f52dd-260e-432f-94de-b47828c3f372",
                ["client_info"] = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "idtoken-0a7f52dd-260e-432f-94de-b47828c3f372-ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "idtoken-0a7f52dd-260e-432f-94de-b47828c3f372-ba6c0d94-a8da-45b2-83ae-33871f9c2dd8";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForRefreshToken_WhenMSSTSResponse_WithB2CAccount()
        {
            var response = TestConstants.CreateB2CTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalRefreshTokenCacheItem(
                MsalEnvironment,
                B2CClientId,
                response,
                homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_rt>",
                ["credential_type"] = "RefreshToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["client_id"] = "0a7f52dd-260e-432f-94de-b47828c3f372",
                ["client_info"] = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "refreshtoken-0a7f52dd-260e-432f-94de-b47828c3f372--";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "refreshtoken-0a7f52dd-260e-432f-94de-b47828c3f372-";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2002, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccount_WhenMSSTSResponse_WithB2CAccount()
        {

            var response = TestConstants.CreateB2CTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalAccountCacheItem(
                MsalEnvironment,
                response.ClientInfo,
                homeAccountId,
                IdToken.Parse(response.IdToken),
                "Missing from the token response",
                B2CTenantId,
                null);

            var expectedJsonObject = new JObject
            {
                ["family_name"] = "SDK Test",
                ["given_name"] = "MSAL",
                ["local_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5",
                ["home_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["username"] = "Missing from the token response",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["authority_type"] = "MSSTS",
                ["name"] = "MSAL SDK Test",
                ["client_info"] = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "ba6c0d94-a8da-45b2-83ae-33871f9c2dd8";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "missing from the token response";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(1003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccessToken_WhenMSSTSResponse_WithB2CAccountAndTenantId()
        {
            var response = TestConstants.CreateB2CTestTokenResponse();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalAccessTokenCacheItem(
                MsalEnvironment,
                B2CClientId,
                response,
                B2CTenantId,
                homeAccountId);
            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload
            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_at>",
                ["target"] = "https://iosmsalb2c.onmicrosoft.com/webapitest/user.read",
                ["extended_expires_on"] = extExpiresOn,
                ["ext_expires_on"] = extExpiresOn,
                ["credential_type"] = "AccessToken",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["expires_on"] = expiresOn,
                ["cached_at"] = cachedAt,
                ["client_id"] = "0a7f52dd-260e-432f-94de-b47828c3f372",
                ["home_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["client_info"] = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            // 2. Verify cache key
            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "accesstoken-0a7f52dd-260e-432f-94de-b47828c3f372-ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-https://iosmsalb2c.onmicrosoft.com/webapitest/user.read";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "accesstoken-0a7f52dd-260e-432f-94de-b47828c3f372-ba6c0d94-a8da-45b2-83ae-33871f9c2dd8";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2001, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForIDToken_WhenMSSTSResponse_WithB2CAccountAndTenantId()
        {
            var response = TestConstants.CreateB2CTestTokenResponseWithTenantId();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalIdTokenCacheItem(
                MsalEnvironment,
                B2CClientId,
                response,
                B2CTenantId,
                homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1Mzg4MDQ4NjAsIm5iZiI6MTUzODgwMTI2MCwidmVyIjoiMS4wIiwiaXNzIjoiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tL2JhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOC92Mi4wLyIsInN1YiI6ImFkMDIwZjhlLWIxYmEtNDRiMi1iZDY5LWMyMmJlODY3MzdmNSIsImF1ZCI6IjBhN2Y1MmRkLTI2MGUtNDMyZi05NGRlLWI0NzgyOGMzZjM3MiIsImlhdCI6MTUzODgwMTI2MCwiYXV0aF90aW1lIjoxNTM4ODAxMjYwLCJpZHAiOiJsaXZlLmNvbSIsIm5hbWUiOiJNU0FMIFNESyBUZXN0Iiwib2lkIjoiYWQwMjBmOGUtYjFiYS00NGIyLWJkNjktYzIyYmU4NjczN2Y1IiwiZmFtaWx5X25hbWUiOiJTREsgVGVzdCIsImdpdmVuX25hbWUiOiJNU0FMIiwiZW1haWxzIjpbIm1zYWxzZGt0ZXN0QG91dGxvb2suY29tIl0sInRmcCI6IkIyQ18xX1NpZ25pbiIsImF0X2hhc2giOiJRNE8zSERDbGNhTGw3eTB1VS1iSkFnIiwidGlkIjoiYmE2YzBkOTQtYThkYS00NWIyLTgzYWUtMzM4NzFmOWMyZGQ4IiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20ifQ.",
                ["credential_type"] = "IdToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["realm"] = "ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["client_id"] = "0a7f52dd-260e-432f-94de-b47828c3f372",
                ["client_info"] = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "idtoken-0a7f52dd-260e-432f-94de-b47828c3f372-ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "idtoken-0a7f52dd-260e-432f-94de-b47828c3f372-ba6c0d94-a8da-45b2-83ae-33871f9c2dd8";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForRefreshToken_WhenMSSTSResponse_WithB2CAccountAndTenantId()
        {
            var response = TestConstants.CreateB2CTestTokenResponseWithTenantId();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalRefreshTokenCacheItem(
                MsalEnvironment,
                B2CClientId,
                TestConstants.CreateB2CTestTokenResponseWithTenantId(),
                homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_rt>",
                ["credential_type"] = "RefreshToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["client_id"] = "0a7f52dd-260e-432f-94de-b47828c3f372",
                ["client_info"] = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "refreshtoken-0a7f52dd-260e-432f-94de-b47828c3f372--";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "refreshtoken-0a7f52dd-260e-432f-94de-b47828c3f372-";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2002, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccount_WhenMSSTSResponse_WithB2CAccountAndTenantId()
        {
            var response = TestConstants.CreateB2CTestTokenResponseWithTenantId();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalAccountCacheItem(
                MsalEnvironment,
                response.ClientInfo,
                homeAccountId,
                IdToken.Parse(response.IdToken),
                "Missing from the token response",
                B2CTenantId,
                null);

            var expectedJsonObject = new JObject
            {
                ["family_name"] = "SDK Test",
                ["given_name"] = "MSAL",
                ["local_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5",
                ["home_account_id"] = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["username"] = "Missing from the token response",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "ba6c0d94-a8da-45b2-83ae-33871f9c2dd8",
                ["authority_type"] = "MSSTS",
                ["name"] = "MSAL SDK Test",
                ["client_info"] = "eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "ba6c0d94-a8da-45b2-83ae-33871f9c2dd8";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "ad020f8e-b1ba-44b2-bd69-c22be86737f5-b2c_1_signin.ba6c0d94-a8da-45b2-83ae-33871f9c2dd8-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "missing from the token response";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(1003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccessToken_WhenMSSTSResponse_WithAADAccountAndFociClient()
        {
            var response = TestConstants.CreateAadTestTokenResponseWithFoci();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalAccessTokenCacheItem(
                MsalEnvironment,
                ClientId,
                response,
                AadTenantId,
                homeAccountId);

            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = DateTimeHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload
            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_at>",
                ["target"] = "Calendars.Read email openid profile Tasks.Read User.Read",
                ["extended_expires_on"] = extExpiresOn,
                ["ext_expires_on"] = extExpiresOn,
                ["credential_type"] = "AccessToken",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["expires_on"] = expiresOn,
                ["cached_at"] = cachedAt,
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["home_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["client_info"] = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            // 2. Verify cache key
            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "accesstoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-calendars.read email openid profile tasks.read user.read";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "accesstoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2001, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForIDToken_WhenMSSTSResponse_WithAADAccountAndFociClient()
        {
            var response = TestConstants.CreateAadTestTokenResponseWithFoci();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalIdTokenCacheItem(
                MsalEnvironment,
                ClientId,
                response,
                AadTenantId,
                homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJiNmM2OWEzNy1kZjk2LTRkYjAtOTA4OC0yYWI5NmUxZDgyMTUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhL3YyLjAiLCJpYXQiOjE1Mzg1Mzg0MjIsIm5iZiI6MTUzODUzODQyMiwiZXhwIjoxNTM4NTQyMzIyLCJuYW1lIjoiQ2xvdWQgSURMQUIgQmFzaWMgVXNlciIsIm9pZCI6IjlmNDg4MGQ4LTgwYmEtNGM0MC05N2JjLWY3YTIzYzcwMzA4NCIsInByZWZlcnJlZF91c2VybmFtZSI6ImlkbGFiQG1zaWRsYWI0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6Ilk2WWtCZEhOTkxITm1US2VsOUtoUno4d3Jhc3hkTFJGaVAxNEJSUFdybjQiLCJ0aWQiOiJmNjQ1YWQ5Mi1lMzhkLTRkMWEtYjUxMC1kMWIwOWE3NGE4Y2EiLCJ1dGkiOiI2bmNpWDAyU01raTlrNzMtRjFzWkFBIiwidmVyIjoiMi4wIn0.",
                ["credential_type"] = "IdToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["realm"] = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["client_info"] = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "idtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "idtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2003, key.iOSType);
        }

        // TODO: We have not, on .NET, implemented saving two different fresh tokens in FOCI case (one FRT and one MRT).  So this test won't succeed.
        // Marking it ignored now until we get through other PRs active for this to get resolved.
        [TestMethod]
        [Ignore]
        public void TestSchemaComplianceForRefreshToken_WhenMSSTSResponse_WithAADAccountAndFociClient()
        {
            var credential = new MsalRefreshTokenCacheItem(MsalEnvironment, ClientId, TestConstants.CreateAadTestTokenResponseWithFoci(), AadTenantId);
            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_rt>",
                ["credential_type"] = "RefreshToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["client_info"] = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0",
                ["family_id"] = "1",
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "refreshtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215--";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "refreshtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2002, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForFamilyRefreshToken_WhenMSSTSResponse_WithAADAccountAndFociClient()
        {
            var response = TestConstants.CreateAadTestTokenResponseWithFoci();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalRefreshTokenCacheItem(
                MsalEnvironment,
                ClientId,
                response,
                homeAccountId: homeAccountId);

            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_rt>",
                ["credential_type"] = "RefreshToken",
                ["environment"] = "login.microsoftonline.com",
                ["home_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["client_info"] = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0",
                ["family_id"] = "1",
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "refreshtoken-1--";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "refreshtoken-1-";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2002, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccount_WhenMSSTSResponse_WithAADAccountAndFociClient()
        {
            var response = TestConstants.CreateAadTestTokenResponseWithFoci();
            string homeAccountId = ClientInfo.CreateFromJson(response.ClientInfo).ToAccountIdentifier();

            var credential = new MsalAccountCacheItem(
                MsalEnvironment,
                response.ClientInfo,
                homeAccountId,
                IdToken.Parse(response.IdToken),
                "idlab@msidlab4.onmicrosoft.com",
                AadTenantId,
                null);

            var expectedJsonObject = new JObject
            {
                ["local_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084",
                ["home_account_id"] = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["username"] = "idlab@msidlab4.onmicrosoft.com",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                ["authority_type"] = "MSSTS",
                ["name"] = "Cloud IDLAB Basic User",
                ["client_info"] = "eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey key = credential.iOSCacheKey;

            string expectedServiceKey = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "idlab@msidlab4.onmicrosoft.com";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(1003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAppMetadata_WhenMSSTSResponse_WithAADAccountAndFociClient()
        {
            var credential = new MsalAppMetadataCacheItem(ClientId, MsalEnvironment, "1");

            var expectedJsonObject = new JObject
            {
                ["client_id"] = "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                ["environment"] = "login.microsoftonline.com",
                ["family_id"] = "1",
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            IiOSKey iOSKey = credential.iOSCacheKey;

            string expectedServiceKey = "appmetadata-b6c69a37-df96-4db0-9088-2ab96e1d8215";
            Assert.AreEqual(expectedServiceKey, iOSKey.iOSService);

            string expectedAccountKey = "login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, iOSKey.iOSAccount);

            string expectedGenericKey = "1";
            Assert.AreEqual(expectedGenericKey, iOSKey.iOSGeneric);

            Assert.AreEqual(3001, iOSKey.iOSType);
        }
    }
}
