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
    internal class ComputeMetadataResponse
    {
        /// <summary>Operating system type (e.g., Windows, Linux).</summary>
        [JsonProperty("osType")]
        public string OsType { get; set; }

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
    internal class ComputeSecurityProfile
    {
        /// <summary>Security type of the VM (e.g., TrustedLaunch, ConfidentialVM).</summary>
        [JsonProperty("securityType")]
        public string SecurityType { get; set; }
    }
}
