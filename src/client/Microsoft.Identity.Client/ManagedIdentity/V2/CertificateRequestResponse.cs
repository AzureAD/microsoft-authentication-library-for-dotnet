// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Buffers.Text;
using System.Net;
#if SUPPORTS_SYSTEM_TEXT_JSON
    using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Represents the response for a Managed Identity CSR request.
    /// </summary>
    internal class CertificateRequestResponse
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; } // client_id of the Managed Identity 

        [JsonProperty("tenant_id")]
        public string TenantId { get; set; } // AAD Tenant of the Managed Identity 

        [JsonProperty("certificate")]
        public string Certificate { get; set; } // Base64 encoded X509certificate

        [JsonProperty("identity_type")]
        public string IdentityType { get; set; } // SAMI or UAMI

        [JsonProperty("mtls_authentication_endpoint")]
        public string MtlsAuthenticationEndpoint { get; set; } // Regional STS mTLS endpoint

        public CertificateRequestResponse() { }

        public static void Validate(CertificateRequestResponse certificateRequestResponse)
        {
            if (string.IsNullOrEmpty(certificateRequestResponse.ClientId) ||
                string.IsNullOrEmpty(certificateRequestResponse.TenantId) ||
                string.IsNullOrEmpty(certificateRequestResponse.Certificate) ||
                string.IsNullOrEmpty(certificateRequestResponse.IdentityType) ||
                string.IsNullOrEmpty(certificateRequestResponse.MtlsAuthenticationEndpoint))
            {
                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    $"[ImdsV2] ImdsV2ManagedIdentitySource.ExecuteCertificateRequestAsync failed because the certificate request response is malformed. Status code: 200",
                    null,
                    ManagedIdentitySource.ImdsV2,
                    (int)HttpStatusCode.OK);
            }

            return true;
        }
    }
}
