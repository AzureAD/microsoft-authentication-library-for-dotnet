using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.Internal.JsonWebToken;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class JsonHelperTests
    {
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
            ClientInfo clientInfo = new ClientInfo() { UniqueObjectIdentifier = "some_uid"};

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
                                       ""kid"": ""lJjBuRyk8s_-oQxT3MgwH5qNS94"",
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
