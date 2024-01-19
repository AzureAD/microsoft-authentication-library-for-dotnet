// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    public class JsonTests
    {
        private readonly string _msalTokenResponseJson;
        private readonly MsalTokenResponse _msalTokenResponse;

        private readonly string _instanceDiscoveryResponseJson;
        private readonly InstanceDiscoveryResponse _instanceDiscoveryResponse;

        private readonly string _oAuth2ResponseBaseJson;
        private readonly OAuth2ResponseBase _oAuth2ResponseBase;

        public JsonTests()
        {
            _oAuth2ResponseBase = InitOAuth2ResponseBase(new OAuth2ResponseBase());
            _oAuth2ResponseBaseJson = JsonHelper.SerializeToJson<OAuth2ResponseBase>(_oAuth2ResponseBase);

            _msalTokenResponse = InitMsalTokenResponse(new MsalTokenResponse());
            _msalTokenResponseJson = JsonHelper.SerializeToJson<MsalTokenResponse>(_msalTokenResponse);

            _instanceDiscoveryResponse = InitInstanceDiscoveryResponse(new InstanceDiscoveryResponse());
            _instanceDiscoveryResponseJson = JsonHelper.SerializeToJson<InstanceDiscoveryResponse>(_instanceDiscoveryResponse);
        }

        private InstanceDiscoveryResponse InitInstanceDiscoveryResponse(InstanceDiscoveryResponse instanceDiscoveryResponse)
        {
            int entries = 30;
            instanceDiscoveryResponse.TenantDiscoveryEndpoint = TestConstants.DiscoveryEndPoint;
            instanceDiscoveryResponse.Metadata = new InstanceDiscoveryMetadataEntry[entries];

            InitOAuth2ResponseBase(instanceDiscoveryResponse);

            for (int i = 0; i < entries; i++)
            {
                instanceDiscoveryResponse.Metadata[i] = new InstanceDiscoveryMetadataEntry
                {
                    Aliases = new[] { "login.windows.net", "login.microsoftonline.com" },
                    PreferredCache = "login.windows.net",
                    PreferredNetwork = "login.microsoftonline.com"
                };
            }

            return instanceDiscoveryResponse;
        }

        private OAuth2ResponseBase InitOAuth2ResponseBase(OAuth2ResponseBase oAuth2ResponseBase)
        {
            oAuth2ResponseBase.Error = "OAuth error";
            oAuth2ResponseBase.SubError = "OAuth suberror";
            oAuth2ResponseBase.ErrorDescription = "OAuth error description";
            oAuth2ResponseBase.ErrorCodes = new[] { "error1", "error2", "error3" };
            oAuth2ResponseBase.CorrelationId = "1234-123-1234";
            oAuth2ResponseBase.Claims = "claim1 claim2";

            return oAuth2ResponseBase;
        }

        private MsalTokenResponse InitMsalTokenResponse(MsalTokenResponse msalTokenResponse)
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

            InitOAuth2ResponseBase(msalTokenResponse);

            return msalTokenResponse;
        }

        [Benchmark]
        public string Serialize_MsalTokenResponse_Test()
        {
            return JsonHelper.SerializeToJson<MsalTokenResponse>(_msalTokenResponse);
        }

        [Benchmark]
        public string Deserialize_MsalTokenResponse_Test()
        {
            return JsonHelper.DeserializeFromJson<MsalTokenResponse>(_msalTokenResponseJson).CorrelationId;
        }

        [Benchmark]
        public string Serialize_InstanceDiscoveryResponse_Test()
        {
            return JsonHelper.SerializeToJson<InstanceDiscoveryResponse>(_instanceDiscoveryResponse);
        }

        [Benchmark]
        public string Deserialize_InstanceDiscoveryResponse_Test()
        {
            return JsonHelper.DeserializeFromJson<InstanceDiscoveryResponse>(_instanceDiscoveryResponseJson).CorrelationId;
        }

        [Benchmark]
        public string Serialize_OAuth2ResponseBase_Test()
        {
            return JsonHelper.SerializeToJson<OAuth2ResponseBase>(_oAuth2ResponseBase);
        }

        [Benchmark]
        public string Deserialize_OAuth2ResponseBase_Test()
        {
            return JsonHelper.DeserializeFromJson<OAuth2ResponseBase>(_oAuth2ResponseBaseJson).CorrelationId;
        }
    }
}
