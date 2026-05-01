// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// Represents an application configuration entry retrieved from Key Vault
    /// and deserialized from the lab infrastructure configuration payload.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Gets or sets the application (client) ID.
        /// </summary>
        [JsonPropertyName("appid")]
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the redirect URI configured for the application.
        /// </summary>
        [JsonPropertyName("redirecturi")]
        public string RedirectUri { get; set; }

        /// <summary>
        /// Gets or sets the authority used to acquire tokens for the application.
        /// </summary>
        [JsonPropertyName("authority")]
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the default scopes requested for the application.
        /// </summary>
        [JsonPropertyName("defaultscopes")]
        public string DefaultScopes { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID associated with the application.
        /// </summary>
        [JsonPropertyName("tenantid")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the target cloud or environment for the application configuration.
        /// </summary>
        [JsonPropertyName("environment")]
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the Key Vault secret name associated with this application configuration.
        /// </summary>
        [JsonPropertyName("secretname")]
        public string SecretName { get; set; }
    }
}
