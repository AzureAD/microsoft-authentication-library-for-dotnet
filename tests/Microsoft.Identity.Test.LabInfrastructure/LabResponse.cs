// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class LabResponse
    {
        [JsonPropertyName("app")]
        public LabApp App { get; set; }

        [JsonPropertyName("user")]
        public LabUser User { get; set; }

        [JsonPropertyName("lab")]
        public Lab Lab { get; set; }
    }

    public class LabApp
    {
        [JsonPropertyName("appid")]
        public string AppId { get; set; }

        // TODO: this is a list, but lab sends a string. Not used today, discuss with lab to return a list
        [JsonPropertyName("redirecturi")]
        public string RedirectUri { get; set; }

        [JsonPropertyName("signinaudience")]
        public string Audience { get; set; }

        // TODO: this is a list, but lab sends a string. Not used today, discuss with lab to return a list
        [JsonPropertyName("authority")]
        public string Authority { get; set; }

    }

    public class Lab
    {
        [JsonPropertyName("tenantid")]
        public string TenantId { get; set; }

        [JsonPropertyName("federationprovider")]
        public FederationProvider FederationProvider { get; set; }

        [JsonPropertyName("credentialvaultkeyname")]
        public string CredentialVaultkeyName { get; set; }

        [JsonPropertyName("authority")]
        public string Authority { get; set; }
    }

    public class LabCredentialResponse
    {
        [JsonPropertyName("Value")]
        public string Secret { get; set; }
    }
}
