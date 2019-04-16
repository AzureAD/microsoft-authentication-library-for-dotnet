// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Instance
{
    internal class TenantDiscoveryResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string AuthorizationEndpoint = "authorization_endpoint";
        public const string TokenEndpoint = "token_endpoint";
        public const string Issuer = "issuer";
    }

    [DataContract]
    internal class TenantDiscoveryResponse : OAuth2ResponseBase
    {
        [DataMember(Name = TenantDiscoveryResponseClaim.AuthorizationEndpoint, IsRequired = false)]
        public string AuthorizationEndpoint { get; set; }

        [DataMember(Name = TenantDiscoveryResponseClaim.TokenEndpoint, IsRequired = false)]
        public string TokenEndpoint { get; set; }

        [DataMember(Name = TenantDiscoveryResponseClaim.Issuer, IsRequired = false)]
        public string Issuer { get; set; }
    }
}
