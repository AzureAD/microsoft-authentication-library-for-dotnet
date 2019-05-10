// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class LabResponse
    {
        [JsonProperty("AppId")]
        public string AppId { get; set; }

        [JsonProperty("Users")]
        public LabUser User { get; set; }
    }
}
