// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Instance
{
    internal class DrsMetadataResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string PassiveAuthEndpoint = "PassiveAuthEndpoint";
        public const string IdentityProviderService = "IdentityProviderService";
    }

    [DataContract]
    internal class IdentityProviderService
    {
        [DataMember(Name = DrsMetadataResponseClaim.PassiveAuthEndpoint, IsRequired = false)]
        public Uri PassiveAuthEndpoint { get; set; }
    }

    [DataContract]
    internal class DrsMetadataResponse : OAuth2ResponseBase
    {
        [DataMember(Name = DrsMetadataResponseClaim.IdentityProviderService, IsRequired = false)]
        public IdentityProviderService IdentityProviderService { get; set; }
    }
}
