// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    //[EtwProfiler] // Uncomment to enable profiler. Info: https://adamsitnik.com/ETW-Profiler/
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

        [BenchmarkCategory("Serialize, MsalTokenResponse"), Benchmark(Description = "Serialize, MsalTokenResponse, With Reflection", Baseline = true)]
        public string Serialization_WithReflection_MsalTokenResponse_Test()
        {
            return JsonHelper.SerializeToJson<MsalTokenResponse>(_msalTokenResponse);
        }

        [BenchmarkCategory("Deserialize, MsalTokenResponse"), Benchmark(Description = "Deserialize, MsalTokenResponse, With Reflection", Baseline = true)]
        public string Deserialization_WithReflection_MsalTokenResponse_Test()
        {
            return JsonHelper.DeserializeFromJson<MsalTokenResponse>(_msalTokenResponseJson).CorrelationId;
        }

        [BenchmarkCategory("Serialize, MsalTokenResponse"), Benchmark(Description = "Serialize, MsalTokenResponse, Without Reflection")]
        public string Serialization_WithoutReflection_MsalTokenResponse_Test()
        {
            return JsonHelper.SerializeNew<MsalTokenResponse>(_msalTokenResponse);
        }

        [BenchmarkCategory("Deserialize, MsalTokenResponse"), Benchmark(Description = "Deserialize, MsalTokenResponse, Without Reflection")]
        public string Deserialization_WithoutReflection_MsalTokenResponse_Test()
        {
            return JsonHelper.DeserializeNew<MsalTokenResponse>(_msalTokenResponseJson).CorrelationId;
        }

        [BenchmarkCategory("Serialize, InstanceDiscoveryResponse"), Benchmark(Description = "Serialize, InstanceDiscoveryResponse, With Reflection", Baseline = true)]
        public string Serialization_WithReflection_InstanceDiscoveryResponse_Test()
        {
            return JsonHelper.SerializeToJson<InstanceDiscoveryResponse>(_instanceDiscoveryResponse);
        }

        [BenchmarkCategory("Deserialize, InstanceDiscoveryResponse"), Benchmark(Description = "Deserialize, InstanceDiscoveryResponse, With Reflection", Baseline = true)]
        public string Deserialization_WithReflection_InstanceDiscoveryResponse_Test()
        {
            return JsonHelper.DeserializeFromJson<InstanceDiscoveryResponse>(_instanceDiscoveryResponseJson).CorrelationId;
        }

        [BenchmarkCategory("Serialize, InstanceDiscoveryResponse"), Benchmark(Description = "Serialize, InstanceDiscoveryResponse, Without Reflection")]
        public string Serialization_WithoutReflection_InstanceDiscoveryResponse_Test()
        {
            return JsonHelper.SerializeNew<InstanceDiscoveryResponse>(_instanceDiscoveryResponse);
        }

        [BenchmarkCategory("Deserialize, InstanceDiscoveryResponse"), Benchmark(Description = "Deserialize, InstanceDiscoveryResponse, Without Reflection")]
        public string Deserialization_WithoutReflection_InstanceDiscoveryResponse_Test()
        {
            return JsonHelper.DeserializeNew<InstanceDiscoveryResponse>(_instanceDiscoveryResponseJson).CorrelationId;
        }

        [BenchmarkCategory("Serialize, OAuth2ResponseBase"), Benchmark(Description = "Serialize, OAuth2ResponseBase, With Reflection", Baseline = true)]
        public string Serialization_WithReflection_OAuth2ResponseBase_Test()
        {
            return JsonHelper.SerializeToJson<OAuth2ResponseBase>(_oAuth2ResponseBase);
        }

        [BenchmarkCategory("Deserialize, OAuth2ResponseBase"), Benchmark(Description = "Deserialize, OAuth2ResponseBase, With Reflection", Baseline = true)]
        public string Deserialization_WithReflection_OAuth2ResponseBase_Test()
        {
            return JsonHelper.DeserializeFromJson<OAuth2ResponseBase>(_oAuth2ResponseBaseJson).CorrelationId;
        }

        [BenchmarkCategory("Serialize, OAuth2ResponseBase"), Benchmark(Description = "Serialize, OAuth2ResponseBase, Without Reflection")]
        public string Serialization_WithoutReflection_OAuth2ResponseBase_Test()
        {
            return JsonHelper.SerializeNew<OAuth2ResponseBase>(_oAuth2ResponseBase);
        }

        [BenchmarkCategory("Deserialize, OAuth2ResponseBase"), Benchmark(Description = "Deserialize, OAuth2ResponseBase, Without Reflection")]
        public string Deserialization_WithoutReflection_OAuth2ResponseBase_Test()
        {
            return JsonHelper.DeserializeNew<OAuth2ResponseBase>(_oAuth2ResponseBaseJson).CorrelationId;
        }
    }
}
