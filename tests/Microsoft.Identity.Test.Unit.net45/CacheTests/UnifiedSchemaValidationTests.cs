// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    // This is meant to stay in sync with iOS code in https://github.com/AzureAD/microsoft-authentication-library-common-for-objc/blob/master/IdentityCore/tests/integration/MSIDCacheSchemaValidationTests.m
    // This is for tests laid out here:  https://identitydivision.visualstudio.com/DevEx/_git/AuthLibrariesApiReview?path=%2FUnifiedSchema%2Ftestcases&version=GBdev

    [TestClass]
    [TestCategory("UnifiedSchema_Validation")]
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

        private MsalTokenResponse CreateAadTestTokenResponse()
        {
            string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Calendars.Read openid profile Tasks.Read User.Read email\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJiNmM2OWEzNy1kZjk2LTRkYjAtOTA4OC0yYWI5NmUxZDgyMTUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhL3YyLjAiLCJpYXQiOjE1Mzg1Mzg0MjIsIm5iZiI6MTUzODUzODQyMiwiZXhwIjoxNTM4NTQyMzIyLCJuYW1lIjoiQ2xvdWQgSURMQUIgQmFzaWMgVXNlciIsIm9pZCI6IjlmNDg4MGQ4LTgwYmEtNGM0MC05N2JjLWY3YTIzYzcwMzA4NCIsInByZWZlcnJlZF91c2VybmFtZSI6ImlkbGFiQG1zaWRsYWI0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6Ilk2WWtCZEhOTkxITm1US2VsOUtoUno4d3Jhc3hkTFJGaVAxNEJSUFdybjQiLCJ0aWQiOiJmNjQ1YWQ5Mi1lMzhkLTRkMWEtYjUxMC1kMWIwOWE3NGE4Y2EiLCJ1dGkiOiI2bmNpWDAyU01raTlrNzMtRjFzWkFBIiwidmVyIjoiMi4wIn0.\",\"client_info\":\"eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        private MsalTokenResponse CreateMsaTestTokenResponse()
        {
            string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Tasks.Read User.Read openid profile\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJ2ZXIiOiIyLjAiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vOTE4ODA0MGQtNmM2Ny00YzViLWIxMTItMzZhMzA0YjY2ZGFkL3YyLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwiYXVkIjoiYjZjNjlhMzctZGY5Ni00ZGIwLTkwODgtMmFiOTZlMWQ4MjE1IiwiZXhwIjoxNTM4ODg1MjU0LCJpYXQiOjE1Mzg3OTg1NTQsIm5iZiI6MTUzODc5ODU1NCwibmFtZSI6IlRlc3QgVXNlcm5hbWUiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJtc2Fsc2RrdGVzdEBvdXRsb29rLmNvbSIsIm9pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInRpZCI6IjkxODgwNDBkLTZjNjctNGM1Yi1iMTEyLTM2YTMwNGI2NmRhZCIsImFpbyI6IkRXZ0tubCFFc2ZWa1NVOGpGVmJ4TTZQaFphUjJFeVhzTUJ5bVJHU1h2UkV1NGkqRm1CVTFSQmw1aEh2TnZvR1NHbHFkQkpGeG5kQXNBNipaM3FaQnIwYzl2YUlSd1VwZUlDVipTWFpqdzghQiIsImFsZyI6IkhTMjU2In0.\",\"client_info\":\"eyJ2ZXIiOiIxLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwibmFtZSI6Ik9sZ2EgRGFsdG9tIiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20iLCJvaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtNDBjMC0zYmFjMTg4ZDAxZDEiLCJ0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQiLCJob21lX29pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInVpZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInV0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQifQ\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        private MsalTokenResponse CreateB2CTestTokenResponse()
        {
            string jsonResponse = "{\"access_token\":\"<removed_at>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1Mzg4MDQ4NjAsIm5iZiI6MTUzODgwMTI2MCwidmVyIjoiMS4wIiwiaXNzIjoiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tL2JhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOC92Mi4wLyIsInN1YiI6ImFkMDIwZjhlLWIxYmEtNDRiMi1iZDY5LWMyMmJlODY3MzdmNSIsImF1ZCI6IjBhN2Y1MmRkLTI2MGUtNDMyZi05NGRlLWI0NzgyOGMzZjM3MiIsImlhdCI6MTUzODgwMTI2MCwiYXV0aF90aW1lIjoxNTM4ODAxMjYwLCJpZHAiOiJsaXZlLmNvbSIsIm5hbWUiOiJNU0FMIFNESyBUZXN0Iiwib2lkIjoiYWQwMjBmOGUtYjFiYS00NGIyLWJkNjktYzIyYmU4NjczN2Y1IiwiZmFtaWx5X25hbWUiOiJTREsgVGVzdCIsImdpdmVuX25hbWUiOiJNU0FMIiwiZW1haWxzIjpbIm1zYWxzZGt0ZXN0QG91dGxvb2suY29tIl0sInRmcCI6IkIyQ18xX1NpZ25pbiIsImF0X2hhc2giOiJRNE8zSERDbGNhTGw3eTB1VS1iSkFnIn0.\",\"token_type\":\"Bearer\",\"not_before\":1538801260,\"expires_in\":3600,\"ext_expires_in\":262800,\"expires_on\":1538804860,\"resource\":\"14df2240-96cc-4f42-a133-ef0807492869\",\"client_info\":\"eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9\",\"scope\":\"https://iosmsalb2c.onmicrosoft.com/webapitest/user.read\",\"refresh_token\":\"<removed_rt>\",\"refresh_token_expires_in\":1209600}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        private MsalTokenResponse CreateB2CTestTokenResponseWithTenantId()
        {
            string jsonResponse = "{\"access_token\":\"<removed_at>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1Mzg4MDQ4NjAsIm5iZiI6MTUzODgwMTI2MCwidmVyIjoiMS4wIiwiaXNzIjoiaHR0cHM6Ly9sb2dpbi5taWNyb3NvZnRvbmxpbmUuY29tL2JhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOC92Mi4wLyIsInN1YiI6ImFkMDIwZjhlLWIxYmEtNDRiMi1iZDY5LWMyMmJlODY3MzdmNSIsImF1ZCI6IjBhN2Y1MmRkLTI2MGUtNDMyZi05NGRlLWI0NzgyOGMzZjM3MiIsImlhdCI6MTUzODgwMTI2MCwiYXV0aF90aW1lIjoxNTM4ODAxMjYwLCJpZHAiOiJsaXZlLmNvbSIsIm5hbWUiOiJNU0FMIFNESyBUZXN0Iiwib2lkIjoiYWQwMjBmOGUtYjFiYS00NGIyLWJkNjktYzIyYmU4NjczN2Y1IiwiZmFtaWx5X25hbWUiOiJTREsgVGVzdCIsImdpdmVuX25hbWUiOiJNU0FMIiwiZW1haWxzIjpbIm1zYWxzZGt0ZXN0QG91dGxvb2suY29tIl0sInRmcCI6IkIyQ18xX1NpZ25pbiIsImF0X2hhc2giOiJRNE8zSERDbGNhTGw3eTB1VS1iSkFnIiwidGlkIjoiYmE2YzBkOTQtYThkYS00NWIyLTgzYWUtMzM4NzFmOWMyZGQ4IiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20ifQ.\",\"token_type\":\"Bearer\",\"not_before\":1538801260,\"expires_in\":3600,\"ext_expires_in\":262800,\"expires_on\":1538804860,\"resource\":\"14df2240-96cc-4f42-a133-ef0807492869\",\"client_info\":\"eyJ1aWQiOiJhZDAyMGY4ZS1iMWJhLTQ0YjItYmQ2OS1jMjJiZTg2NzM3ZjUtYjJjXzFfc2lnbmluIiwidXRpZCI6ImJhNmMwZDk0LWE4ZGEtNDViMi04M2FlLTMzODcxZjljMmRkOCJ9\",\"scope\":\"https://iosmsalb2c.onmicrosoft.com/webapitest/user.read\",\"refresh_token\":\"<removed_rt>\",\"refresh_token_expires_in\":1209600}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

        private MsalTokenResponse CreateAadTestTokenResponseWithFoci()
        {
            string jsonResponse = "{\"token_type\":\"Bearer\",\"scope\":\"Calendars.Read openid profile Tasks.Read User.Read email\",\"expires_in\":3600,\"ext_expires_in\":262800,\"access_token\":\"<removed_at>\",\"refresh_token\":\"<removed_rt>\",\"id_token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJiNmM2OWEzNy1kZjk2LTRkYjAtOTA4OC0yYWI5NmUxZDgyMTUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhL3YyLjAiLCJpYXQiOjE1Mzg1Mzg0MjIsIm5iZiI6MTUzODUzODQyMiwiZXhwIjoxNTM4NTQyMzIyLCJuYW1lIjoiQ2xvdWQgSURMQUIgQmFzaWMgVXNlciIsIm9pZCI6IjlmNDg4MGQ4LTgwYmEtNGM0MC05N2JjLWY3YTIzYzcwMzA4NCIsInByZWZlcnJlZF91c2VybmFtZSI6ImlkbGFiQG1zaWRsYWI0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6Ilk2WWtCZEhOTkxITm1US2VsOUtoUno4d3Jhc3hkTFJGaVAxNEJSUFdybjQiLCJ0aWQiOiJmNjQ1YWQ5Mi1lMzhkLTRkMWEtYjUxMC1kMWIwOWE3NGE4Y2EiLCJ1dGkiOiI2bmNpWDAyU01raTlrNzMtRjFzWkFBIiwidmVyIjoiMi4wIn0.\",\"client_info\":\"eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0\",\"foci\":\"1\"}";
            var msalTokenResponse = JsonHelper.DeserializeFromJson<MsalTokenResponse>(jsonResponse);
            return msalTokenResponse;
        }

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
            var credential = new MsalAccessTokenCacheItem(MsalEnvironment, ClientId, CreateAadTestTokenResponse(), AadTenantId);
            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = CoreHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload

            // TODO: ios tests do not have client_info key in them.  Should they?
            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_at>",
                ["target"] = "Calendars.Read openid profile Tasks.Read User.Read email",
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
            MsalAccessTokenCacheKey key = credential.GetKey();

            string expectedServiceKey = "accesstoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-calendars.read openid profile tasks.read user.read email";
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
            var credential = new MsalIdTokenCacheItem(MsalEnvironment, ClientId, CreateAadTestTokenResponse(), AadTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalIdTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalRefreshTokenCacheItem(MsalEnvironment, ClientId, CreateAadTestTokenResponse(), AadTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalRefreshTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalAccountCacheItem(MsalEnvironment, CreateAadTestTokenResponse(), "idlab@msidlab4.onmicrosoft.com", AadTenantId);
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

            MsalAccountCacheKey key = credential.GetKey();

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
            var credential = new MsalAccessTokenCacheItem(MsalEnvironment, ClientId, CreateMsaTestTokenResponse(), MsaTenantId);
            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = CoreHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload

            // TODO: ios tests do not have client_info key in them.  Should they?
            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_at>",
                ["target"] = "Tasks.Read User.Read openid profile",
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
            MsalAccessTokenCacheKey key = credential.GetKey();

            string expectedServiceKey = "accesstoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-9188040d-6c67-4c5b-b112-36a304b66dad-tasks.read user.read openid profile";
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
            var credential = new MsalIdTokenCacheItem(MsalEnvironment, ClientId, CreateMsaTestTokenResponse(), MsaTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalIdTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalRefreshTokenCacheItem(MsalEnvironment, ClientId, CreateMsaTestTokenResponse(), MsaTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalRefreshTokenCacheKey key = credential.GetKey();

            string expectedServiceKey = "refreshtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215--";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "00000000-0000-0000-40c0-3bac188d01d1.9188040d-6c67-4c5b-b112-36a304b66dad-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "refreshtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2002, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccount_WhenMSSTSResponse_WithMSAAccount()
        {
            // TODO:  This test is failing because the ID token in the iOS sample is "longblobofstuff>."  We parse the value of the id token out of the part 
            // AFTER the . and that's empty.  Need to investigate what's going on here.

            var credential = new MsalAccountCacheItem(MsalEnvironment, CreateMsaTestTokenResponse(), "msalsdktest@outlook.com", MsaTenantId);
            var expectedJsonObject = new JObject
            {
                ["local_account_id"] = "00000000-0000-0000-40c0-3bac188d01d1",
                ["home_account_id"] = "00000000-0000-0000-40c0-3bac188d01d1.9188040d-6c67-4c5b-b112-36a304b66dad",
                ["username"] = "msalsdktest@outlook.com",
                ["environment"] = "login.microsoftonline.com",
                ["realm"] = "9188040d-6c67-4c5b-b112-36a304b66dad",
                ["authority_type"] = "MSSTS",
                ["name"] = "Test Username",
                ["client_info"] = "eyJ2ZXIiOiIxLjAiLCJzdWIiOiJBQUFBQUFBQUFBQUFBQUFBQUFBQUFNTmVBRnBTTGdsSGlPVHI5SVpISkVBIiwibmFtZSI6Ik9sZ2EgRGFsdG9tIiwicHJlZmVycmVkX3VzZXJuYW1lIjoibXNhbHNka3Rlc3RAb3V0bG9vay5jb20iLCJvaWQiOiIwMDAwMDAwMC0wMDAwLTAwMDAtNDBjMC0zYmFjMTg4ZDAxZDEiLCJ0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQiLCJob21lX29pZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInVpZCI6IjAwMDAwMDAwLTAwMDAtMDAwMC00MGMwLTNiYWMxODhkMDFkMSIsInV0aWQiOiI5MTg4MDQwZC02YzY3LTRjNWItYjExMi0zNmEzMDRiNjZkYWQifQ"
            };

            AssertAreJsonStringsEquivalent(expectedJsonObject.ToString(), credential.ToJsonString());

            MsalAccountCacheKey key = credential.GetKey();

            string expectedServiceKey = "9188040d-6c67-4c5b-b112-36a304b66dad";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "00000000-0000-0000-40c0-3bac188d01d1.9188040d-6c67-4c5b-b112-36a304b66dad-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "msalsdktest@outlook.com";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(1003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForAccessToken_WhenMSSTSResponse_WithB2CAccount()
        {
            var credential = new MsalAccessTokenCacheItem(MsalEnvironment, B2CClientId, CreateB2CTestTokenResponse(), B2CTenantId);
            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = CoreHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload

            // TODO: ios tests do not have client_info key in them.  Should they?
            // TODO: ios tests do not have extended_expires_on, and it's not in the payload.  is this correct?
            //    For the moment, I've added it to the B2C payload so that we get proper values here.
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
            MsalAccessTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalIdTokenCacheItem(MsalEnvironment, B2CClientId, CreateB2CTestTokenResponse(), B2CTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalIdTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalRefreshTokenCacheItem(MsalEnvironment, B2CClientId, CreateB2CTestTokenResponse(), B2CTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalRefreshTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalAccountCacheItem(MsalEnvironment, CreateB2CTestTokenResponse(), "Missing from the token response", B2CTenantId);
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

            MsalAccountCacheKey key = credential.GetKey();

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
            var credential = new MsalAccessTokenCacheItem(MsalEnvironment, B2CClientId, CreateB2CTestTokenResponseWithTenantId(), B2CTenantId);
            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = CoreHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload

            // TODO: ios tests do not have client_info key in them.  Should they?
            // TODO: ios tests do not have extended_expires_on, and it's not in the payload.  is this correct?
            //    For the moment, I've added it to the B2C payload so that we get proper values here.
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
            MsalAccessTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalIdTokenCacheItem(MsalEnvironment, B2CClientId, CreateB2CTestTokenResponseWithTenantId(), B2CTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalIdTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalRefreshTokenCacheItem(MsalEnvironment, B2CClientId, CreateB2CTestTokenResponseWithTenantId(), B2CTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalRefreshTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalAccountCacheItem(MsalEnvironment, CreateB2CTestTokenResponseWithTenantId(), "Missing from the token response", B2CTenantId);
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

            MsalAccountCacheKey key = credential.GetKey();

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
            var credential = new MsalAccessTokenCacheItem(MsalEnvironment, ClientId, CreateAadTestTokenResponseWithFoci(), AadTenantId);
            DateTime currentDate = DateTime.UtcNow;
            string expiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(3600));
            string extExpiresOn = CoreHelpers.DateTimeToUnixTimestamp(currentDate.AddSeconds(262800));
            string cachedAt = CoreHelpers.DateTimeToUnixTimestamp(currentDate);

            // 1. Verify payload

            // TODO: ios tests do not have client_info key in them.  Should they?
            var expectedJsonObject = new JObject
            {
                ["secret"] = "<removed_at>",
                ["target"] = "Calendars.Read openid profile Tasks.Read User.Read email",
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
            MsalAccessTokenCacheKey key = credential.GetKey();

            string expectedServiceKey = "accesstoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-calendars.read openid profile tasks.read user.read email";
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
            var credential = new MsalIdTokenCacheItem(MsalEnvironment, ClientId, CreateAadTestTokenResponseWithFoci(), AadTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalIdTokenCacheKey key = credential.GetKey();

            string expectedServiceKey = "idtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "9f4880d8-80ba-4c40-97bc-f7a23c703084.f645ad92-e38d-4d1a-b510-d1b09a74a8ca-login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "idtoken-b6c69a37-df96-4db0-9088-2ab96e1d8215-f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(2003, key.iOSType);
        }

        [TestMethod]
        public void TestSchemaComplianceForRefreshToken_WhenMSSTSResponse_WithAADAccountAndFociClient()
        {
            // TODO: this test is failing because we don't create a refresh token AND a family refresh token in .NET.  So the keys are wrong since this 
            // will become a FRT.  Need to align this with iOS folks.

            var credential = new MsalRefreshTokenCacheItem(MsalEnvironment, ClientId, CreateAadTestTokenResponseWithFoci(), AadTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalRefreshTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalRefreshTokenCacheItem(MsalEnvironment, ClientId, CreateAadTestTokenResponseWithFoci(), AadTenantId);

            // TODO: ios tests do not have client_info key in them.  Should they?
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

            MsalRefreshTokenCacheKey key = credential.GetKey();

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
            var credential = new MsalAccountCacheItem(MsalEnvironment, CreateAadTestTokenResponseWithFoci(), "idlab@msidlab4.onmicrosoft.com", AadTenantId);
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

            MsalAccountCacheKey key = credential.GetKey();

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

            MsalAppMetadataCacheKey key = credential.GetKey();

            string expectedServiceKey = "appmetadata-b6c69a37-df96-4db0-9088-2ab96e1d8215";
            Assert.AreEqual(expectedServiceKey, key.iOSService);

            string expectedAccountKey = "login.microsoftonline.com";
            Assert.AreEqual(expectedAccountKey, key.iOSAccount);

            string expectedGenericKey = "1";
            Assert.AreEqual(expectedGenericKey, key.iOSGeneric);

            Assert.AreEqual(3001, key.iOSType);
        }
    }
}
