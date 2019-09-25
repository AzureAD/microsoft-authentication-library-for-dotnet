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
        public LabUser2 User { get; set; }

        [JsonProperty("lab")]
        public Lab Lab { get; set; }
    }

    public class LabApp
    {
        [JsonProperty("appid")]
        public string AppId { get; set; }
    }

    public class Lab
    {
        public string CredentialVaultkeyName { get; set; }
    }
}
