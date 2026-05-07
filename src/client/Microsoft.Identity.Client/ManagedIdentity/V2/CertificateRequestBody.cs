// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal class CertificateRequestBody
    {
        [JsonProperty("csr")]
        public string Csr { get; set; }

        [JsonProperty("attestation_token")]
        [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string AttestationToken { get; set; }

        public static bool IsNullOrEmpty(CertificateRequestBody certificateRequestBody)
        {
            return certificateRequestBody == null ||
                   (string.IsNullOrEmpty(certificateRequestBody.Csr) && string.IsNullOrEmpty(certificateRequestBody.AttestationToken));
        }
    }
}
