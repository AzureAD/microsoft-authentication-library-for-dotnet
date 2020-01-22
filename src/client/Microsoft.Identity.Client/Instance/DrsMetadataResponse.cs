// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
    internal class DrsMetadataResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string PassiveAuthEndpoint = "PassiveAuthEndpoint";
        public const string IdentityProviderService = "IdentityProviderService";
    }

    [JsonObject]
#if ANDROID || iOS
    [Preserve(AllMembers = true)]
#endif
    internal class IdentityProviderService
    {
        [JsonProperty(PropertyName = DrsMetadataResponseClaim.PassiveAuthEndpoint)]
        public Uri PassiveAuthEndpoint { get; set; }
    }

    [JsonObject]
#if ANDROID || iOS
    [Preserve(AllMembers = true)]
#endif
    internal class DrsMetadataResponse : OAuth2ResponseBase
    {
        [JsonProperty(PropertyName = DrsMetadataResponseClaim.IdentityProviderService)]
        public IdentityProviderService IdentityProviderService { get; set; }
    }
}
