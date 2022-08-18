// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Kerberos;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client.Utils
{
    [JsonSerializable(typeof(KerberosSupplementalTicket))]
    [JsonSerializable(typeof(InstanceDiscoveryResponse))]
    [JsonSerializable(typeof(LocalImdsErrorResponse))]
    [JsonSerializable(typeof(AdalResultWrapper))]
    [JsonSerializable(typeof(List<KeyValuePair<string, IEnumerable<string>>>))]
    [JsonSerializable(typeof(ClientInfo))]
    [JsonSerializable(typeof(OAuth2ResponseBase))]
    [JsonSerializable(typeof(MsalTokenResponse))]
    [JsonSerializable(typeof(UserRealmDiscoveryResponse))]
    [JsonSerializable(typeof(DeviceCodeResponse))]
    [JsonSerializable(typeof(AdfsWebFingerResponse))]
    [JsonSerializable(typeof(JsonWebToken.JWTHeaderWithCertificate))]
    [JsonSerializable(typeof(JsonWebToken.JWTPayload))]
    [JsonSerializable(typeof(DeviceAuthHeader))]
    [JsonSerializable(typeof(DeviceAuthPayload))]
#if ANDROID
    [JsonSerializable(typeof(Microsoft.Identity.Client.Platforms.Android.Broker.BrokerRequest))]
#endif
#if iOS
    [JsonSerializable(typeof(Platforms.iOS.IntuneEnrollmentIdHelper.EnrollmentIDs))]
#endif
    [JsonSourceGenerationOptions]
    internal partial class MsalJsonSerializerContext : JsonSerializerContext
    {
        private static MsalJsonSerializerContext s_customContext;

        public static MsalJsonSerializerContext Custom
        {
            get
            {
                return s_customContext ??=
                    new MsalJsonSerializerContext(new JsonSerializerOptions
                    {
                        NumberHandling = JsonNumberHandling.AllowReadingFromString,
                        AllowTrailingCommas = true,
                        Converters =
                        {
                            new JsonStringConverter(),
                        }
                    });
            }
        }
    }
}
