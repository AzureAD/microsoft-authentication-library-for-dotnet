// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class LabResponse
    {
        [JsonProperty("app")]
        public LabApp App { get; set; }

        [JsonProperty("user")]
        public LabUser User { get; set; }

        [JsonProperty("lab")]
        public Lab Lab { get; set; }
    }

    public class LabApp
    {
        [JsonProperty("appid")]
        public string AppId { get; set; }

        // TODO: this is a list, but lab sends a string. Not used today, discuss with lab to return a list
        [JsonProperty("redirecturi")]
        public string RedirectUri { get; set; }

        [JsonProperty("signinaudience")]
        public string Audience { get; set; }

        // TODO: this is a list, but lab sends a string. Not used today, discuss with lab to return a list
        [JsonProperty("authority")]
        public string Authority { get; set; }

        [JsonProperty("tenantid")]
        public string TenantId { get; set; }
    }

    public class Lab
    {
        [JsonProperty("tenantid")]
        public string TenantId { get; set; }

        [JsonProperty("federationprovider")]
        public FederationProvider FederationProvider { get; set; }

        [JsonProperty("credentialvaultkeyname")]
        public string CredentialVaultkeyName { get; set; }

        [JsonProperty("authority")]
        public string Authority { get; set; }
    }

    public class LabCredentialResponse
    {
        [JsonProperty("Value")]
        public string Secret { get; set; }
    }
}
