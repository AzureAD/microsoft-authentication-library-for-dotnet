// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal class AdfsWebFingerResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string Subject = "subject";
        public const string Links = "links";
        public const string Rel = "rel";
        public const string Href = "href";
    }

    [JsonObject(Title = AdfsWebFingerResponseClaim.Links)]
    [Preserve(AllMembers = true)]
    internal class LinksList
    {
        [JsonProperty(PropertyName = AdfsWebFingerResponseClaim.Rel)]
        public string Rel { get; set; }

        [JsonProperty(PropertyName = AdfsWebFingerResponseClaim.Href)]
        public string Href { get; set; }
    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class AdfsWebFingerResponse : OAuth2ResponseBase
    {
        [JsonProperty(PropertyName = AdfsWebFingerResponseClaim.Subject)]
        public string Subject { get; set; }

        [JsonProperty(PropertyName = AdfsWebFingerResponseClaim.Links)]
        public List<LinksList> Links { get; set; }
    }
}
