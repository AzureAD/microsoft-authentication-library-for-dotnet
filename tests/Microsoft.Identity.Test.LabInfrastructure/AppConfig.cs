// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// Represents application configuration retrieved from Key Vault.
    /// </summary>
    public class AppConfig
    {
        [JsonProperty("appid")]
        public string AppId { get; set; }

        [JsonProperty("redirecturi")]
        public string RedirectUri { get; set; }

        [JsonProperty("authority")]
        public string Authority { get; set; }

        [JsonProperty("defaultscopes")]
        public string DefaultScopes { get; set; }

        [JsonProperty("tenantid")]
        public string TenantId { get; set; }

        [JsonProperty("environment")]
        public string Environment { get; set; }

        [JsonProperty("secretname")]
        public string SecretName { get; set; }
    }
}
