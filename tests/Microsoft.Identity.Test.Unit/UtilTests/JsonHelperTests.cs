// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.Internal.JsonWebToken;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class JsonHelperTests
    {

        [TestMethod]
        public void Deserialize_AdalResultWrapper()
        {
            string json = @"{
                           ""RawClientInfo"": ""eyJ1aWQiOiI5ZjQ4ODBkOC04MGJhLTRjNDAtOTdiYy1mN2EyM2M3MDMwODQiLCJ1dGlkIjoiZjY0NWFkOTItZTM4ZC00ZDFhLWI1MTAtZDFiMDlhNzRhOGNhIn0"",
                           ""RefreshToken"": ""rt_secret"",
                           ""ResourceInResponse"": ""email openid profile https://graph.microsoft.com/Agreement.Read.All https://graph.microsoft.com/Agreement.ReadWrite.All https://graph.microsoft.com/AgreementAcceptance.Read https://graph.microsoft.com/AgreementAcceptance.Read.All https://graph.microsoft.com/AllSites.FullControl https://graph.microsoft.com/AllSites.Manage https://graph.microsoft.com/AllSites.Read https://graph.microsoft.com/AllSites.Write https://graph.microsoft.com/AppCatalog.ReadWrite.All https://graph.microsoft.com/AuditLog.Read.All https://graph.microsoft.com/Bookings.Manage.All https://graph.microsoft.com/Bookings.Read.All https://graph.microsoft.com/Bookings.ReadWrite.All https://graph.microsoft.com/BookingsAppointment.ReadWrite.All https://graph.microsoft.com/Calendars.Read https://graph.microsoft.com/Calendars.Read.All https://graph.microsoft.com/Calendars.Read.Shared https://graph.microsoft.com/Calendars.ReadWrite https://graph.microsoft.com/Calendars.ReadWrite.All https://graph.microsoft.com/Calendars.ReadWrite.Shared https://graph.microsoft.com/Contacts.Read https://graph.microsoft.com/Contacts.Read.All https://graph.microsoft.com/Contacts.Read.Shared https://graph.microsoft.com/Contacts.ReadWrite https://graph.microsoft.com/Contacts.ReadWrite.All https://graph.microsoft.com/Contacts.ReadWrite.Shared https://graph.microsoft.com/Device.Command https://graph.microsoft.com/Device.Read https://graph.microsoft.com/DeviceManagementApps.Read.All https://graph.microsoft.com/DeviceManagementApps.ReadWrite.All https://graph.microsoft.com/DeviceManagementConfiguration.Read.All https://graph.microsoft.com/DeviceManagementConfiguration.ReadWrite.All https://graph.microsoft.com/DeviceManagementManagedDevices.PrivilegedOperations.All https://graph.microsoft.com/DeviceManagementManagedDevices.Read.All https://graph.microsoft.com/DeviceManagementManagedDevices.ReadWrite.All https://graph.microsoft.com/DeviceManagementRBAC.Read.All https://graph.microsoft.com/DeviceManagementRBAC.ReadWrite.All https://graph.microsoft.com/DeviceManagementServiceConfig.Read.All https://graph.microsoft.com/DeviceManagementServiceConfig.ReadWrite.All https://graph.microsoft.com/Directory.AccessAsUser.All https://graph.microsoft.com/Directory.Read.All https://graph.microsoft.com/Directory.ReadWrite.All https://graph.microsoft.com/EAS.AccessAsUser.All https://graph.microsoft.com/EduAdministration.Read https://graph.microsoft.com/EduAdministration.ReadWrite https://graph.microsoft.com/EduAssignments.Read https://graph.microsoft.com/EduAssignments.ReadBasic https://graph.microsoft.com/EduAssignments.ReadWrite https://graph.microsoft.com/EduAssignments.ReadWriteBasic https://graph.microsoft.com/EduRoster.Read https://graph.microsoft.com/EduRoster.ReadBasic https://graph.microsoft.com/EduRoster.ReadWrite https://graph.microsoft.com/EWS.AccessAsUser.All https://graph.microsoft.com/Exchange.Manage https://graph.microsoft.com/Files.Read https://graph.microsoft.com/Files.Read.All https://graph.microsoft.com/Files.Read.Selected https://graph.microsoft.com/Files.ReadWrite https://graph.microsoft.com/Files.ReadWrite.All https://graph.microsoft.com/Files.ReadWrite.AppFolder https://graph.microsoft.com/Files.ReadWrite.Selected https://graph.microsoft.com/Financials.ReadWrite.All https://graph.microsoft.com/Group.Read.All https://graph.microsoft.com/Group.ReadWrite.All https://graph.microsoft.com/IdentityProvider.Read.All https://graph.microsoft.com/IdentityProvider.ReadWrite.All https://graph.microsoft.com/IdentityRiskEvent.Read.All https://graph.microsoft.com/Mail.Read https://graph.microsoft.com/Mail.Read.All https://graph.microsoft.com/Mail.Read.Shared https://graph.microsoft.com/Mail.ReadWrite https://graph.microsoft.com/Mail.ReadWrite.All https://graph.microsoft.com/Mail.ReadWrite.Shared https://graph.microsoft.com/Mail.Send https://graph.microsoft.com/Mail.Send.All https://graph.microsoft.com/Mail.Send.Shared https://graph.microsoft.com/MailboxSettings.Read https://graph.microsoft.com/MailboxSettings.ReadWrite https://graph.microsoft.com/Member.Read.Hidden https://graph.microsoft.com/MyFiles.Read https://graph.microsoft.com/MyFiles.Write https://graph.microsoft.com/Notes.Create https://graph.microsoft.com/Notes.Read https://graph.microsoft.com/Notes.Read.All https://graph.microsoft.com/Notes.ReadWrite https://graph.microsoft.com/Notes.ReadWrite.All https://graph.microsoft.com/Notes.ReadWrite.CreatedByApp https://graph.microsoft.com/People.Read https://graph.microsoft.com/People.Read.All https://graph.microsoft.com/People.ReadWrite https://graph.microsoft.com/PrivilegedAccess.ReadWrite.AzureAD https://graph.microsoft.com/PrivilegedAccess.ReadWrite.AzureResources https://graph.microsoft.com/Reports.Read.All https://graph.microsoft.com/SecurityEvents.Read.All https://graph.microsoft.com/SecurityEvents.ReadWrite.All https://graph.microsoft.com/Sites.FullControl.All https://graph.microsoft.com/Sites.Manage.All https://graph.microsoft.com/Sites.Read.All https://graph.microsoft.com/Sites.ReadWrite.All https://graph.microsoft.com/Sites.Search.All https://graph.microsoft.com/Subscription.Read.All https://graph.microsoft.com/Tasks.Read https://graph.microsoft.com/Tasks.Read.Shared https://graph.microsoft.com/Tasks.ReadWrite https://graph.microsoft.com/Tasks.ReadWrite.Shared https://graph.microsoft.com/TermStore.Read.All https://graph.microsoft.com/TermStore.ReadWrite.All https://graph.microsoft.com/User.Export.All https://graph.microsoft.com/User.Invite.All https://graph.microsoft.com/User.Read https://graph.microsoft.com/User.Read.All https://graph.microsoft.com/User.ReadBasic.All https://graph.microsoft.com/User.ReadWrite https://graph.microsoft.com/User.ReadWrite.All https://graph.microsoft.com/UserActivity.ReadWrite.CreatedByApp https://graph.microsoft.com/UserTimelineActivity.Write.CreatedByApp"",
                           ""Result"": {
                              ""AccessToken"": null,
                              ""AccessTokenType"": null,
                              ""ExpiresOn"": {
                                 ""DateTime"": ""/Date(-62135596800000)/"",
                                 ""OffsetMinutes"": 0
                              },
                              ""ExtendedExpiresOn"": {
                                 ""DateTime"": ""/Date(-62135596800000)/"",
                                 ""OffsetMinutes"": 0
                              },
                              ""ExtendedLifeTimeToken"": false,
                              ""IdToken"": null,
                              ""TenantId"": null,
                              ""UserInfo"": {
                                 ""DisplayableId"": ""idlab@msidlab4.onmicrosoft.com"",
                                 ""FamilyName"": null,
                                 ""GivenName"": null,
                                 ""IdentityProvider"": null,
                                 ""PasswordChangeUrl"": null,
                                 ""PasswordExpiresOn"": null,
                                 ""UniqueId"": ""9f4880d8-80ba-4c40-97bc-f7a23c703084""
                              }
                           },
                           ""UserAssertionHash"": null
                        }";

            AdalResultWrapper result = JsonHelper.DeserializeFromJson<AdalResultWrapper>(json);
            Assert.AreEqual("idlab@msidlab4.onmicrosoft.com", result.Result.UserInfo.DisplayableId);
            Assert.AreEqual("rt_secret", result.RefreshToken);
        }

        [TestMethod]
        public void Serialize_ClientInfo()
        {
            ClientInfo clientInfo = new ClientInfo() { UniqueObjectIdentifier = "some_uid", UniqueTenantIdentifier = "some_tid" };

            string actualJson = JsonHelper.SerializeToJson(clientInfo);
            string expectedJson = @"{
                                       ""uid"": ""some_uid"",
                                       ""utid"": ""some_tid""
                                    }";

            JsonTestUtils.AssertJsonDeepEquals(expectedJson, actualJson);
        }

        [TestMethod]
        public void Serialize_ClientInfo_WithNull()
        {
            ClientInfo clientInfo = new ClientInfo() { UniqueObjectIdentifier = "some_uid" };

            string actualJson = JsonHelper.SerializeToJson(clientInfo);
            string expectedJson = @"{
                                       ""uid"": ""some_uid"",
                                       ""utid"": null
                                    }";

            JsonTestUtils.AssertJsonDeepEquals(expectedJson, actualJson);
        }

        [TestMethod]
        public void Serialize_OldDictionaryTokenCache()
        {
            const string AccessTokenKey = "access_tokens";
            const string RefreshTokenKey = "refresh_tokens";
            const string IdTokenKey = "id_tokens";
            const string AccountKey = "accounts";

            var cacheDict = new Dictionary<string, IEnumerable<string>>
            {
                [AccessTokenKey] = new List<string>() { "at1", "at2" },
                [RefreshTokenKey] = new List<string>() { "rt" },
                [IdTokenKey] = new List<string>() { "idt" },
                [AccountKey] = new List<string>() { "acc1", "acc2" },
            };

            var cacheKeyValueList = cacheDict.ToList();

            string actualJson = JsonHelper.SerializeToJson(cacheKeyValueList);
            string expectedJson = @"[
                                       {
                                          ""Key"": ""access_tokens"",
                                          ""Value"": [
                                             ""at1"",
                                             ""at2""
                                          ]
                                       },
                                       {
                                          ""Key"": ""refresh_tokens"",
                                          ""Value"": [
                                             ""rt""
                                          ]
                                       },
                                       {
                                          ""Key"": ""id_tokens"",
                                          ""Value"": [
                                             ""idt""
                                          ]
                                       },
                                       {
                                          ""Key"": ""accounts"",
                                          ""Value"": [
                                             ""acc1"",
                                             ""acc2""
                                          ]
                                       }
                                    ]";

            JsonTestUtils.AssertJsonDeepEquals(expectedJson, actualJson);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\RSATestCertDotNet.pfx")]
        public void Serialize_Jwt()
        {
            var payload = new JWTPayload
            {
                Audience = "aud",
                Issuer = "iss",
                ValidFrom = 123,
                ValidTo = 124,
                Subject = "123-456-789",
                JwtIdentifier = "321-654"
            };

            var certificate = new X509Certificate2(
                   ResourceHelper.GetTestResourceRelativePath("RSATestCertDotNet.pfx"));

            var header = new JWTHeaderWithCertificate(certificate, Base64UrlHelpers.Encode(certificate.GetCertHash()), true);
            string actualPayload = JsonHelper.SerializeToJson(payload);
            string actualHeader = JsonHelper.SerializeToJson(header);

            string expectedPayload = @"{
                                       ""aud"": ""aud"",
                                       ""exp"": 124,
                                       ""iss"": ""iss"",
                                       ""jti"": ""321-654"",
                                       ""nbf"": 123,
                                       ""sub"": ""123-456-789""
                                    }";

            string expectedHeader = @"{
                                       ""alg"": ""RS256"",
                                       ""typ"": ""JWT"",
                                       ""x5t"": ""lJjBuRyk8s_-oQxT3MgwH5qNS94"",
                                       ""kid"": ""9498C1B91CA4F2CFFEA10C53DCC8301F9A8D4BDE"",
                                       ""x5c"": ""MIIDJDCCAgygAwIBAgIQK4SCZgh/R5anP05v4z6VLjANBgkqhkiG9w0BAQsFADAPMQ0wCwYDVQQDEwRUZXN0MB4XDTE5MDgxNTE3MjY1M1oXDTIwMDgxNTE3MzY1M1owDzENMAsGA1UEAxMEVGVzdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAIxSuzLrpxnq44CSux3l2UMvIBwBXnh4tmmZtju4qCNJzVmCrhyC9i5jH7YCicXeFQChWfbZpyo2TpDD/cTw+Rpi9QLhhGvDnMF+uk1pqSp5Fdh11YacX7w76Wc7Er+FM2PiKtyDX6+nFzUvV3SfjfdcAadConDAWOdmpd34UNZ/DzM6dRKynWuaE+0kD843Tr+pCXlMGQBAQatWyROK+rgOKhnv1/vMAZ90SCjxAhnjxj+9GRIGYzonuTa+EOqXRn1XQ+j54Ux953Oq0zGCNbXndGjGKH1U1JP/nAemFsh0h2DcdAdEkxOS3+QrdiZEkPPfe8x5BLJmvoRWJ9eCAT0CAwEAAaN8MHowDgYDVR0PAQH/BAQDAgWgMAkGA1UdEwQCMAAwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMB8GA1UdIwQYMBaAFB03ltXqrZeIzolZQj8w98DG8HCIMB0GA1UdDgQWBBQdN5bV6q2XiM6JWUI/MPfAxvBwiDANBgkqhkiG9w0BAQsFAAOCAQEAiXAQHHWiJ+8wLk0evDZSXDfQ0brYsKLimxJSrVOzpz4BnHTIr86ZEYA6jCKNfhRnrPU9HQ43CUSU1MRX03ovdJMoYjuWCGAFlZrYMC9PhPwt2B0a3DRl0wsl3jxOYYrFHonBWvjDFdWEP2Nr2T8iWPgpS5uIdgU1GqN9EbI+3B46qH4rTH3vAwpeF38XDjBO8DYycotwG34zgD2zQ2ZoPmQG07Y8rjBo+JW56ri3RfeMu3kZVfM359JXzQhw+L8PDY8MVhltiZ1ufvKS6F5vAZYLUXUGtVmlS7mLgNJKvJN9fxd1BlZdqfD3+o4xBUGVCjS3HR/7NJBl/pPHZtKckQ==""
                                    }";

            JsonTestUtils.AssertJsonDeepEquals(expectedPayload, actualPayload);
            JsonTestUtils.AssertJsonDeepEquals(expectedHeader, actualHeader);
        }

        [TestMethod]
        public void Deserialize_TokenResponse()
        {
            string json = @"{
                               ""token_type"": ""Bearer"",
                               ""scope"": ""user_impersonation"",
                               ""expires_in"": ""3600"",
                               ""ext_expires_in"": ""3600"",
                               ""expires_on"": ""1566165638"",
                               ""not_before"": ""1566161738"",
                               ""resource"": ""user.read"",
                               ""access_token"": ""at_secret"",
                               ""refresh_token"": ""rt_secret"",
                               ""id_token"": ""idtoken"",
                               ""client_info"": ""eyJ1aWQiOiI2ZWVkYTNhMS1jM2I5LTRlOTItYTk0ZC05NjVhNTBjMDZkZTciLCJ1dGlkIjoiNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3In0""
                            }";

            MsalTokenResponse response = JsonHelper.DeserializeFromJson<MsalTokenResponse>(json);

            Assert.AreEqual("Bearer", response.TokenType);
            Assert.AreEqual("user_impersonation", response.Scope);
            Assert.AreEqual(3600, response.ExpiresIn);
            Assert.AreEqual(3600, response.ExtendedExpiresIn);
            Assert.AreEqual("idtoken", response.IdToken);
            Assert.AreEqual("rt_secret", response.RefreshToken);
            Assert.AreEqual("at_secret", response.AccessToken);
            Assert.AreEqual("eyJ1aWQiOiI2ZWVkYTNhMS1jM2I5LTRlOTItYTk0ZC05NjVhNTBjMDZkZTciLCJ1dGlkIjoiNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3In0", response.ClientInfo);

        }
    }

}
