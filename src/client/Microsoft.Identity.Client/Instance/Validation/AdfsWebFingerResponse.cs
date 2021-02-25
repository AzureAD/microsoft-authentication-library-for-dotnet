// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

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
    internal class LinksList : IJsonSerializable<LinksList>
    {
        [JsonProperty(PropertyName = AdfsWebFingerResponseClaim.Rel)]
        public string Rel { get; set; }

        [JsonProperty(PropertyName = AdfsWebFingerResponseClaim.Href)]
        public string Href { get; set; }

        public LinksList DeserializeFromJson(string json) => DeserializeFromJObject(JObject.Parse(json));

        public LinksList DeserializeFromJObject(JObject jObject)
        {
            Rel = jObject[AdfsWebFingerResponseClaim.Rel]?.ToString();
            Href = jObject[AdfsWebFingerResponseClaim.Href]?.ToString();

            return this;
        }

        public string SerializeToJson() => SerializeToJObject().ToString(Formatting.None);

        public JObject SerializeToJObject()
        {
            return new JObject(
                new JProperty(AdfsWebFingerResponseClaim.Rel, Rel),
                new JProperty(AdfsWebFingerResponseClaim.Href, Href));
        }
    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class AdfsWebFingerResponse : OAuth2ResponseBase, IJsonSerializable<AdfsWebFingerResponse>
    {
        [JsonProperty(PropertyName = AdfsWebFingerResponseClaim.Subject)]
        public string Subject { get; set; }

        [JsonProperty(PropertyName = AdfsWebFingerResponseClaim.Links)]
        public List<LinksList> Links { get; set; }

        public new AdfsWebFingerResponse DeserializeFromJson(string json) => DeserializeFromJObject(JObject.Parse(json));


        public new AdfsWebFingerResponse DeserializeFromJObject(JObject jObject)
        {
            Subject = jObject[AdfsWebFingerResponseClaim.Subject]?.ToString();
            Links = jObject[AdfsWebFingerResponseClaim.Links] != null ? ((JArray)jObject[AdfsWebFingerResponseClaim.Links]).Select(c => new LinksList().DeserializeFromJObject((JObject)c)).ToList() : null;
            base.DeserializeFromJObject(jObject);

            return this;

        }

        public new string SerializeToJson() => SerializeToJObject().ToString(Formatting.None);

        public new JObject SerializeToJObject()
        {
            return new JObject(
                new JProperty(AdfsWebFingerResponseClaim.Subject, Subject),
                new JProperty(AdfsWebFingerResponseClaim.Links, new JArray(Links.Select(i => i.SerializeToJObject()))),
                base.SerializeToJObject().Properties());
        }
    }
}
