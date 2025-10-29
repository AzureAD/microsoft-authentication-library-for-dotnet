// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_SYSTEM_TEXT_JSON
    using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
    using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal class CertificateRequestBody
    {
        [JsonProperty("csr")]
        public string Csr { get; set; }

        [JsonProperty("attestation_token")]
        public string AttestationToken { get; set; }

        public static bool IsNullOrEmpty(CertificateRequestBody certificateRequestBody)
        {
            return certificateRequestBody == null ||
                   (string.IsNullOrEmpty(certificateRequestBody.Csr) && string.IsNullOrEmpty(certificateRequestBody.AttestationToken));
        }
    }
}
