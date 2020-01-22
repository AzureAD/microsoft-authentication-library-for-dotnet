// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json;


namespace Microsoft.Identity.Client.Instance
{
    internal class TenantDiscoveryResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string AuthorizationEndpoint = "authorization_endpoint";
        public const string TokenEndpoint = "token_endpoint";
        public const string Issuer = "issuer";
    }

    [JsonObject]
    [Preserve]
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
