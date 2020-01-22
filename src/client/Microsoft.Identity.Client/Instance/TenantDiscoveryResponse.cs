// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json;
#if iOS
using Foundation;
#endif
#if ANDROID
using Android.Runtime;
#endif

namespace Microsoft.Identity.Client.Instance
{
    internal class TenantDiscoveryResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string AuthorizationEndpoint = "authorization_endpoint";
        public const string TokenEndpoint = "token_endpoint";
        public const string Issuer = "issuer";
    }

    [JsonObject]
#if ANDROID || iOS
    [Preserve(AllMembers = true)]
#endif
    internal class TenantDiscoveryResponse : OAuth2ResponseBase
    {
        [JsonProperty(PropertyName = TenantDiscoveryResponseClaim.AuthorizationEndpoint)]
        public string AuthorizationEndpoint { get; set; }

        [JsonProperty(PropertyName = TenantDiscoveryResponseClaim.TokenEndpoint)]
        public string TokenEndpoint { get; set; }

        [JsonProperty(PropertyName = TenantDiscoveryResponseClaim.Issuer)]
        public string Issuer { get; set; }
    }
}
