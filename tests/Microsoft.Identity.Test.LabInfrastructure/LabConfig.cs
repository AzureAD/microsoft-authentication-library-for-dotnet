// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// Represents lab environment configuration retrieved from Key Vault.
    /// </summary>
    public class LabConfig
    {
        [JsonProperty("tenantid")]
        public string TenantId { get; set; }

        [JsonProperty("authority")]
        public string Authority { get; set; }
    }
}
