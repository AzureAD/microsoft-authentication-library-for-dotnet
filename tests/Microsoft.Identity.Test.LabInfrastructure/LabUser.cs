// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class LabUser
    {
        [JsonProperty("objectId")]
        public Guid ObjectId { get; set; }

        [JsonProperty("userType")]
        public UserType UserType { get; set; }

        [JsonProperty("upn")]
        public string Upn { get; set; }

        [JsonProperty("displayname")]
        public string DisplayName { get; set; }

        [JsonProperty("mfa")]
        public MFA Mfa { get; set; }

        [JsonProperty("protectionpolicy")]
        public ProtectionPolicy ProtectionPolicy { get; set; }

        [JsonProperty("homedomain")]
        public HomeDomain HomeDomain { get; set; }

        [JsonProperty("homeupn")]
        public string HomeUPN { get; set; }

        [JsonProperty("b2cprovider")]
        public B2CIdentityProvider B2cProvider { get; set; }

        [JsonProperty("labname")]
        public string LabName { get; set; }

        public FederationProvider FederationProvider { get; set; }

        public string Credential { get; set; }

        public string TenantId { get; set; }

        private string _password = null;

        [JsonProperty("appid")]
        public string AppId { get; set; }

        public string GetOrFetchPassword()
        {
            if (_password == null)
            {
                _password = LabUserHelper.FetchUserPassword(LabName);
            }

            return _password;
        }
    }
}
