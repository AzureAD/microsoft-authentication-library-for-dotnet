// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_SYSTEM_TEXT_JSON
    using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
    using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
#else
    using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal class CertificateRequestBody
    {
        [JsonProperty("csr")]
        public string Csr { get; set; }

#if SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty("attestation_token")]
        [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
#else
        [JsonProperty("attestation_token", NullValueHandling = NullValueHandling.Ignore)]
#endif
        public string AttestationToken { get; set; }

        public static bool IsNullOrEmpty(CertificateRequestBody certificateRequestBody)
        {
            return certificateRequestBody == null ||
                   (string.IsNullOrEmpty(certificateRequestBody.Csr) && string.IsNullOrEmpty(certificateRequestBody.AttestationToken));
        }
    }
}
