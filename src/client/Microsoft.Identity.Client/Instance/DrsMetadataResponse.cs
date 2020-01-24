// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Instance
{
    internal class DrsMetadataResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string PassiveAuthEndpoint = "PassiveAuthEndpoint";
        public const string IdentityProviderService = "IdentityProviderService";
    }

    [DataContract]
    [JsonObject]
    [Preserve]
    internal class IdentityProviderService
    {
        [JsonProperty(PropertyName = DrsMetadataResponseClaim.PassiveAuthEndpoint)]
        public Uri PassiveAuthEndpoint { get; set; }
    }

    [DataContract]
    [JsonObject]
    [Preserve]
    internal class DrsMetadataResponse : OAuth2ResponseBase
    {
        [JsonProperty(PropertyName = DrsMetadataResponseClaim.IdentityProviderService)]
        public IdentityProviderService IdentityProviderService { get; set; }
    }
}
