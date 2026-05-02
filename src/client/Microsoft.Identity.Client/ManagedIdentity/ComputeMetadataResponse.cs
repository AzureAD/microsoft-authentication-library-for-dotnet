// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Platforms.net;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Represents compute metadata retrieved from the Azure Instance Metadata Service (IMDS).
    /// </summary>
    [JsonObject]
    [Preserve(AllMembers = true)]
    public class ComputeMetadataResponse
    {
        /// <summary>Azure cloud environment (e.g., AzurePublicCloud).</summary>
        [JsonProperty("azEnvironment")]
        public string AzEnvironment { get; set; }

        /// <summary>Azure region of the VM.</summary>
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>Name of the VM resource.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Operating system type (e.g., Windows, Linux).</summary>
        [JsonProperty("osType")]
        public string OsType { get; set; }

        /// <summary>Resource group containing the VM.</summary>
        [JsonProperty("resourceGroupName")]
        public string ResourceGroupName { get; set; }

        /// <summary>Full ARM resource ID of the VM.</summary>
        [JsonProperty("resourceId")]
        public string ResourceId { get; set; }

        /// <summary>Azure subscription ID.</summary>
        [JsonProperty("subscriptionId")]
        public string SubscriptionId { get; set; }

        /// <summary>Unique identifier of the VM.</summary>
        [JsonProperty("vmId")]
        public string VmId { get; set; }

        /// <summary>VM SKU size (e.g., Standard_D4s_v5).</summary>
        [JsonProperty("vmSize")]
        public string VmSize { get; set; }

        /// <summary>
        /// Security profile indicating platform security posture. May be null when IMDS
        /// does not return security profile information for the current VM.
        /// </summary>
        [JsonProperty("securityProfile")]
        public ComputeSecurityProfile SecurityProfile { get; set; }
    }

    /// <summary>
    /// Represents the security profile of an Azure VM from IMDS compute metadata.
    /// </summary>
    [JsonObject]
    [Preserve(AllMembers = true)]
    public class ComputeSecurityProfile
    {
        /// <summary>Whether Secure Boot is enabled on the VM, when returned by IMDS.</summary>
        [JsonProperty("secureBootEnabled")]
        public string SecureBootEnabled { get; set; }

        /// <summary>Whether a virtual TPM is enabled on the VM, when returned by IMDS.</summary>
        [JsonProperty("virtualTpmEnabled")]
        public string VirtualTpmEnabled { get; set; }
    }
}
