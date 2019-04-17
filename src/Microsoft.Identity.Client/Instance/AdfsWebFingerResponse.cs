// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Instance
{
    internal class AdfsWebFingerResponseClaim : OAuth2ResponseBaseClaim
    {
        public const string Subject = "subject";
        public const string Links = "links";
        public const string Rel = "rel";
        public const string Href = "href";
    }

    [DataContract(Name = AdfsWebFingerResponseClaim.Links)]
    internal class LinksList
    {
        [DataMember(Name = AdfsWebFingerResponseClaim.Rel, IsRequired = false)]
        public string Rel { get; set; }

        [DataMember(Name = AdfsWebFingerResponseClaim.Href, IsRequired = false)]
        public string Href { get; set; }
    }

    [DataContract]
    internal class AdfsWebFingerResponse : OAuth2ResponseBase
    {
        [DataMember(Name = AdfsWebFingerResponseClaim.Subject, IsRequired = false)]
        public string Subject { get; set; }

        [DataMember(Name = AdfsWebFingerResponseClaim.Links, IsRequired = false)]
        public List<LinksList> Links { get; set; }
    }
}
