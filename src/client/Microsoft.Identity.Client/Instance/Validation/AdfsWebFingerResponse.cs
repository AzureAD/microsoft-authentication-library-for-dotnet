// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.Json;
using System.Text.Json.Nodes;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal class AdfsWebFingerResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string Subject = "subject";
        public const string Links = "links";
        public const string Rel = "rel";
        public const string Href = "href";
    }
    
    [Preserve(AllMembers = true)]
    internal class LinksList
    {
        [JsonProperty(AdfsWebFingerResponseClaim.Rel)]
        public string Rel { get; set; }

        [JsonProperty(AdfsWebFingerResponseClaim.Href)]
        public string Href { get; set; }
    }
    
    [Preserve(AllMembers = true)]
    internal class AdfsWebFingerResponse : OAuth2ResponseBase
    {
        [JsonProperty(AdfsWebFingerResponseClaim.Subject)]
        public string Subject { get; set; }

        [JsonProperty(AdfsWebFingerResponseClaim.Links)]
        public List<LinksList> Links { get; set; }
    }
}
