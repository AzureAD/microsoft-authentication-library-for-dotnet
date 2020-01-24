// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Identity.Json;

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

    [DataContract]
    [JsonObject]
    [Preserve]
    internal class OAuth2ResponseBase
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
    }
}
