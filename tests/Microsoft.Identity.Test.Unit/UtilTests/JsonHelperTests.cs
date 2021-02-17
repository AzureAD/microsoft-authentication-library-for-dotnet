using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Instance.Validation;
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
            DateTimeOffset dateTimeOffset = DateTimeOffset.MinValue;

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

            var header = new JWTHeaderWithCertificate(ClientCredentialWrapper.CreateWithCertificate(certificate), true);
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

        #region ISerializable Tests
        [TestMethod]
        public void IJsonSerializable_OAuth2ResponseBase_Test()
        {
            // Arrange
            OAuth2ResponseBase expected = SetOAuth2ResponseBaseProperties(new OAuth2ResponseBase());

            // Act - serialize
            string jsonSerializedLegacy = JsonHelper.SerializeToJson<OAuth2ResponseBase>(expected);
            string jsonSerializedNew = JsonHelper.SerializeNew<OAuth2ResponseBase>(expected);

            // Assert serialization
            Assert.AreEqual(jsonSerializedLegacy, jsonSerializedNew);

            // Act - deserialize
            OAuth2ResponseBase objectDeserializedNew = JsonHelper.DeserializeNew<OAuth2ResponseBase>(jsonSerializedLegacy);

            // Assert deserialization
            AssertOAuth2ResponseBaseProperties(expected, objectDeserializedNew);
        }

        [TestMethod]
        public void IJsonSerializable_InstanceDiscoveryMetadataEntry_Test()
        {
            // Arrange
            InstanceDiscoveryMetadataEntry expected = SetInstanceDiscoveryMetadataEntryProperties(new InstanceDiscoveryMetadataEntry());

            // Act - serialize
            string jsonSerializedLegacy = JsonHelper.SerializeToJson<InstanceDiscoveryMetadataEntry>(expected);
            string jsonSerializedNew = JsonHelper.SerializeNew<InstanceDiscoveryMetadataEntry>(expected);

            // Assert serialization
            Assert.AreEqual(jsonSerializedLegacy, jsonSerializedNew);

            // Act - deserialize
            InstanceDiscoveryMetadataEntry objectDeserializedNew = JsonHelper.DeserializeNew<InstanceDiscoveryMetadataEntry>(jsonSerializedLegacy);

            // Assert deserialization
            AssertInstanceDiscoveryMetadataEntryProperties(expected, objectDeserializedNew);
        }

        [TestMethod]
        public void IJsonSerializable_InstanceDiscoveryResponse_Test()
        {
            // Arrange
            InstanceDiscoveryResponse expected = SetInstanceDiscoveryResponseProperties(new InstanceDiscoveryResponse());

            // Act - serialize
            string jsonSerializedLegacy = JsonHelper.SerializeToJson<InstanceDiscoveryResponse>(expected);
            string jsonSerializedNew = JsonHelper.SerializeNew<InstanceDiscoveryResponse>(expected);

            // Assert serialization
            Assert.AreEqual(jsonSerializedLegacy, jsonSerializedNew);

            // Act - deserialize
            InstanceDiscoveryResponse objectDeserializedNew = JsonHelper.DeserializeNew<InstanceDiscoveryResponse>(jsonSerializedLegacy);

            // Assert deserialization
            AssertInstanceDiscoveryResponseProperties(expected, objectDeserializedNew);
        }

        [TestMethod]
        public void IJsonSerializable_DeviceCodeResponse_Test()
        {
            // Arrange
            DeviceCodeResponse expected = SetDeviceCodeResponseProperties(new DeviceCodeResponse());

            // Act - serialize
            string jsonSerializedLegacy = JsonHelper.SerializeToJson<DeviceCodeResponse>(expected);
            string jsonSerializedNew = JsonHelper.SerializeNew<DeviceCodeResponse>(expected);

            // Assert serialization
            Assert.AreEqual(jsonSerializedLegacy, jsonSerializedNew);

            // Act - deserialize
            DeviceCodeResponse objectDeserializedNew = JsonHelper.DeserializeNew<DeviceCodeResponse>(jsonSerializedLegacy);

            // Assert deserialization
            AssertDeviceCodeResponseProperties(expected, objectDeserializedNew);
        }

        [TestMethod]
        public void IJsonSerializable_MsalTokenResponse_Test()
        {
            MsalTokenResponse expected = SetMsalTokenResponseProperties(new MsalTokenResponse());

            // Act - serialize
            string jsonSerializedLegacy = JsonHelper.SerializeToJson<MsalTokenResponse>(expected);
            string jsonSerializedNew = JsonHelper.SerializeNew<MsalTokenResponse>(expected);

            // Assert serialization
            Assert.AreEqual(jsonSerializedLegacy, jsonSerializedNew);

            // Act - deserialize
            MsalTokenResponse objectDeserializedNew = JsonHelper.DeserializeNew<MsalTokenResponse>(jsonSerializedLegacy);

            // Assert deserialization
            AssertMsalTokenResponseProperties(expected, objectDeserializedNew);
        }

        [TestMethod]
        public void IJsonSerializable_LinksList_Test()
        {
            var expected = new LinksList()
            {
                Rel = "rel1",
                Href = "href1"
            };

            var jsonSerializedLegacy = JsonHelper.SerializeToJson<LinksList>(expected);
            var jsonSerializedNew = JsonHelper.SerializeNew<LinksList>(expected);

            // Assert serialization
            Assert.AreEqual(jsonSerializedLegacy, jsonSerializedNew);

            var objectDeserializedNew = JsonHelper.DeserializeNew<LinksList>(jsonSerializedLegacy);

            // Assert deserialization
            Assert.AreEqual(expected.Rel, objectDeserializedNew.Rel);
            Assert.AreEqual(expected.Href, objectDeserializedNew.Href);
        }

        [TestMethod]
        public void IJsonSerializable_AdfsWebFingerResponse_Test()
        {
            var expected = new AdfsWebFingerResponse()
            {
                Subject = "adfs subject",
                Links = new List<LinksList>
                {
                    new LinksList()
                    {
                        Rel = "rel1",
                        Href = "href1"
                    },
                    new LinksList()
                    {
                        Rel = "rel2",
                        Href = "href2"
                    }
                },
                Error = "OAuth error",
                SubError = "OAuth suberror",
                ErrorDescription = "OAuth error description",
                ErrorCodes = new[] { "error1", "error2", "error3" },
                CorrelationId = "1234-123-1234",
                Claims = "claim1 claim2"
            };

            var jsonSerializedLegacy = JsonHelper.SerializeToJson<AdfsWebFingerResponse>(expected);
            var jsonSerializedNew = JsonHelper.SerializeNew<AdfsWebFingerResponse>(expected);

            // Assert serialization
            Assert.AreEqual(jsonSerializedLegacy, jsonSerializedNew);

            var objectDeserializedNew = JsonHelper.DeserializeNew<AdfsWebFingerResponse>(jsonSerializedLegacy);

            // Assert deserialization
            Assert.AreEqual(expected.Subject, objectDeserializedNew.Subject);
            Assert.AreEqual(expected.Links.Count, objectDeserializedNew.Links.Count);
            // AssertCollectionItemsEqualByValue(expected.Links, objectDeserializedManually.Links);
            Assert.AreEqual(expected.Error, objectDeserializedNew.Error);
            Assert.AreEqual(expected.SubError, objectDeserializedNew.SubError);
            Assert.AreEqual(expected.ErrorDescription, objectDeserializedNew.ErrorDescription);
            CollectionAssert.AreEqual(expected.ErrorCodes, objectDeserializedNew.ErrorCodes);
            Assert.AreEqual(expected.CorrelationId, objectDeserializedNew.CorrelationId);
            Assert.AreEqual(expected.Claims, objectDeserializedNew.Claims);
        }

        private OAuth2ResponseBase SetOAuth2ResponseBaseProperties(OAuth2ResponseBase oAuth2ResponseBase)
        {
            oAuth2ResponseBase.Error = "OAuth error";
            oAuth2ResponseBase.SubError = "OAuth suberror";
            oAuth2ResponseBase.ErrorDescription = "OAuth error description";
            oAuth2ResponseBase.ErrorCodes = new[] { "error1", "error2", "error3" };
            oAuth2ResponseBase.CorrelationId = "1234-123-1234";
            oAuth2ResponseBase.Claims = "claim1 claim2";

            return oAuth2ResponseBase;
        }

        private void AssertOAuth2ResponseBaseProperties(OAuth2ResponseBase expected, OAuth2ResponseBase actual)
        {
            Assert.AreEqual(expected.Error, actual.Error);
            Assert.AreEqual(expected.SubError, actual.SubError);
            Assert.AreEqual(expected.ErrorDescription, actual.ErrorDescription);
            CollectionAssert.AreEqual(expected.ErrorCodes, actual.ErrorCodes);
            Assert.AreEqual(expected.CorrelationId, actual.CorrelationId);
            Assert.AreEqual(expected.Claims, actual.Claims);
        }

        private InstanceDiscoveryMetadataEntry SetInstanceDiscoveryMetadataEntryProperties(InstanceDiscoveryMetadataEntry instanceDiscoveryMetadataEntry)
        {
            instanceDiscoveryMetadataEntry.Aliases = new[] { "login.windows.net", "login.microsoftonline.com" };
            instanceDiscoveryMetadataEntry.PreferredCache = "login.windows.net";
            instanceDiscoveryMetadataEntry.PreferredNetwork = "login.microsoftonline.com";

            return instanceDiscoveryMetadataEntry;
        }

        private void AssertInstanceDiscoveryMetadataEntryProperties(InstanceDiscoveryMetadataEntry expected, InstanceDiscoveryMetadataEntry actual)
        {
            Assert.AreEqual(expected.PreferredCache, actual.PreferredCache);
            Assert.AreEqual(expected.PreferredNetwork, actual.PreferredNetwork);
            CollectionAssert.AreEqual(expected.Aliases, actual.Aliases);
        }

        private InstanceDiscoveryResponse SetInstanceDiscoveryResponseProperties(InstanceDiscoveryResponse instanceDiscoveryResponse)
        {
            instanceDiscoveryResponse.TenantDiscoveryEndpoint = TestConstants.DiscoveryEndPoint;
            instanceDiscoveryResponse.Metadata = new[]
            {
                SetInstanceDiscoveryMetadataEntryProperties(new InstanceDiscoveryMetadataEntry())
            };
            SetOAuth2ResponseBaseProperties(instanceDiscoveryResponse);

            return instanceDiscoveryResponse;
        }

        private void AssertInstanceDiscoveryResponseProperties(InstanceDiscoveryResponse expected, InstanceDiscoveryResponse actual)
        {
            Assert.AreEqual(expected.TenantDiscoveryEndpoint, actual.TenantDiscoveryEndpoint);
            Assert.AreEqual(expected.Metadata.Length, actual.Metadata.Length);
            for (int i = 0; i < expected.Metadata.Length; i++)
            {
                AssertInstanceDiscoveryMetadataEntryProperties(expected.Metadata[i], actual.Metadata[i]);
            }
            AssertOAuth2ResponseBaseProperties(expected, actual);
        }

        private DeviceCodeResponse SetDeviceCodeResponseProperties(DeviceCodeResponse deviceCodeResponse)
        {
            deviceCodeResponse.UserCode = "user code";
            deviceCodeResponse.DeviceCode = "device code";
            deviceCodeResponse.VerificationUrl = "verification url";
            deviceCodeResponse.VerificationUri = "verification uri";
            deviceCodeResponse.ExpiresIn = 1234;
            deviceCodeResponse.Interval = 12345;
            deviceCodeResponse.Message = "device message";
            SetOAuth2ResponseBaseProperties(deviceCodeResponse);

            return deviceCodeResponse;
        }

        private void AssertDeviceCodeResponseProperties(DeviceCodeResponse expected, DeviceCodeResponse actual)
        {
            Assert.AreEqual(expected.UserCode, actual.UserCode);
            Assert.AreEqual(expected.DeviceCode, actual.DeviceCode);
            Assert.AreEqual(expected.VerificationUrl, actual.VerificationUrl);
            Assert.AreEqual(expected.VerificationUri, actual.VerificationUri);
            Assert.AreEqual(expected.ExpiresIn, actual.ExpiresIn);
            Assert.AreEqual(expected.Interval, actual.Interval);
            Assert.AreEqual(expected.Message, actual.Message);
            AssertOAuth2ResponseBaseProperties(expected, actual);
        }

        private MsalTokenResponse SetMsalTokenResponseProperties(MsalTokenResponse msalTokenResponse)
        {
            msalTokenResponse.TokenType = "token type";
            msalTokenResponse.AccessToken = "access token";
            msalTokenResponse.RefreshToken = "refresh token";
            msalTokenResponse.Scope = "scope scope";
            msalTokenResponse.ClientInfo = "client info";
            msalTokenResponse.IdToken = "id token";
            msalTokenResponse.ExpiresIn = 123;
            msalTokenResponse.ExtendedExpiresIn = 12345;
            msalTokenResponse.RefreshIn = 12333;
            msalTokenResponse.FamilyId = "family id";

            SetOAuth2ResponseBaseProperties(msalTokenResponse);

            return msalTokenResponse;
        }

        private void AssertMsalTokenResponseProperties(MsalTokenResponse expected, MsalTokenResponse actual)
        {
            Assert.AreEqual(expected.TokenType, actual.TokenType);
            Assert.AreEqual(expected.AccessToken, actual.AccessToken);
            Assert.AreEqual(expected.RefreshToken, actual.RefreshToken);
            Assert.AreEqual(expected.Scope, actual.Scope);
            Assert.AreEqual(expected.ClientInfo, actual.ClientInfo);
            Assert.AreEqual(expected.IdToken, actual.IdToken);
            Assert.AreEqual(expected.ExpiresIn, actual.ExpiresIn);
            Assert.AreEqual(expected.ExtendedExpiresIn, actual.ExtendedExpiresIn);
            Assert.AreEqual(expected.RefreshIn, actual.RefreshIn);
            Assert.AreEqual(expected.FamilyId, actual.FamilyId);
            AssertOAuth2ResponseBaseProperties(expected, actual);
        }
        #endregion
    }
}
