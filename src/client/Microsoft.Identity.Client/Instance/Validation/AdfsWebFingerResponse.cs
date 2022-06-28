// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Identity.Client.OAuth2;

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
        [JsonPropertyName(AdfsWebFingerResponseClaim.Rel)]
        public string Rel { get; set; }

        [JsonPropertyName(AdfsWebFingerResponseClaim.Href)]
        public string Href { get; set; }
    }

    [Preserve(AllMembers = true)]
    internal class AdfsWebFingerResponse : OAuth2ResponseBase
    {
        [JsonPropertyName(AdfsWebFingerResponseClaim.Subject)]
        public string Subject { get; set; }

        [JsonPropertyName(AdfsWebFingerResponseClaim.Links)]
        public List<LinksList> Links { get; set; }
    }
}
