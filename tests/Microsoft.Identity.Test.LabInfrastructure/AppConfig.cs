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

        // TODO: this is a list, but lab sends a string. Not used today, discuss with lab to return a list
        [JsonProperty("redirecturi")]
        public string RedirectUri { get; set; }

        // TODO: this is a list, but lab sends a string. Not used today, discuss with lab to return a list
        [JsonProperty("authority")]
        public string Authority { get; set; }

        [JsonProperty("defaultscopes")]
        public string DefaultScopes { get; set; }
    }
}
