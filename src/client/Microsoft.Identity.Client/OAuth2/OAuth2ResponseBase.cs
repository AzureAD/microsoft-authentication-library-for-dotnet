// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.OAuth2
{
    internal class OAuth2ResponseBaseClaim
    {
        public const string Claims = "claims";
        public const string Error = "error";
        public const string SubError = "suberror";
        public const string ErrorDescription = "error_description";
        public const string ErrorCodes = "error_codes";
        public const string CorrelationId = "correlation_id";
    }

    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class OAuth2ResponseBase : IJsonSerializable<OAuth2ResponseBase>
    {
        [JsonProperty(PropertyName = OAuth2ResponseBaseClaim.Error)]
        public string Error { get; set; }

        [JsonProperty(PropertyName = OAuth2ResponseBaseClaim.SubError)]
        public string SubError { get; set; }

        [JsonProperty(PropertyName = OAuth2ResponseBaseClaim.ErrorDescription)]
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Do not expose these in the MsalException because Evo does not guarantee that the error
        /// codes remain the same.
        /// </summary>
        [JsonProperty(PropertyName = OAuth2ResponseBaseClaim.ErrorCodes)]
        public string[] ErrorCodes { get; set; }

        [JsonProperty(PropertyName = OAuth2ResponseBaseClaim.CorrelationId)]
        public string CorrelationId { get; set; }

        [JsonProperty(PropertyName = OAuth2ResponseBaseClaim.Claims)]
        public string Claims { get; set; }

        public OAuth2ResponseBase DeserializeFromJson(string json) => DeserializeFromJObject(JObject.Parse(json));

        public OAuth2ResponseBase DeserializeFromJObject(JObject jObject)
        {
            Error = jObject[OAuth2ResponseBaseClaim.Error]?.ToString();
            SubError = jObject[OAuth2ResponseBaseClaim.SubError]?.ToString();
            ErrorDescription = jObject[OAuth2ResponseBaseClaim.ErrorDescription]?.ToString();
            ErrorCodes = jObject[OAuth2ResponseBaseClaim.ErrorCodes] != null ? ((JArray)jObject[OAuth2ResponseBaseClaim.ErrorCodes]).Select(c => (string)c).ToArray() : null;
            CorrelationId = jObject[OAuth2ResponseBaseClaim.CorrelationId]?.ToString();
            Claims = jObject[OAuth2ResponseBaseClaim.Claims]?.ToString();

            return this;
        }

        public string SerializeToJson() => SerializeToJObject().ToString(Formatting.None);

        public JObject SerializeToJObject()
        {
            return new JObject(
                new JProperty(OAuth2ResponseBaseClaim.Error, Error),
                new JProperty(OAuth2ResponseBaseClaim.SubError, SubError),
                new JProperty(OAuth2ResponseBaseClaim.ErrorDescription, ErrorDescription),
                new JProperty(OAuth2ResponseBaseClaim.ErrorCodes, new JArray(ErrorCodes)),
                new JProperty(OAuth2ResponseBaseClaim.CorrelationId, CorrelationId),
                new JProperty(OAuth2ResponseBaseClaim.Claims, Claims));
        }
    }
}
