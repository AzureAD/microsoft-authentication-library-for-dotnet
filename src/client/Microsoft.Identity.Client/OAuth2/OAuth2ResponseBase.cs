// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

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
    internal class OAuth2ResponseBase
    {
        [JsonProperty(OAuth2ResponseBaseClaim.Error)]
        public string Error { get; set; }

        [JsonProperty(OAuth2ResponseBaseClaim.SubError)]
        public string SubError { get; set; }

        [JsonProperty(OAuth2ResponseBaseClaim.ErrorDescription)]
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Do not expose these in the MsalException because Evo does not guarantee that the error
        /// codes remain the same.
        /// </summary>
        [JsonProperty(OAuth2ResponseBaseClaim.ErrorCodes)]
        public string[] ErrorCodes { get; set; }

        [JsonProperty(OAuth2ResponseBaseClaim.CorrelationId)]
        public string CorrelationId { get; set; }

        [JsonProperty(OAuth2ResponseBaseClaim.Claims)]
        public string Claims { get; set; }
    }
}
