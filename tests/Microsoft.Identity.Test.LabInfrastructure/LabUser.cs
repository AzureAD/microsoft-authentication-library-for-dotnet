// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class LabUser
    {
        [JsonPropertyName("objectId")]
        public Guid ObjectId { get; set; }

        [JsonPropertyName("userType")]
        public UserType UserType { get; set; }

        [JsonPropertyName("upn")]
        public string Upn { get; set; }

        [JsonPropertyName("displayname")]
        public string DisplayName { get; set; }

        [JsonPropertyName("mfa")]
        public MFA Mfa { get; set; }

        [JsonPropertyName("protectionpolicy")]
        public ProtectionPolicy ProtectionPolicy { get; set; }

        [JsonPropertyName("homedomain")]
        public HomeDomain HomeDomain { get; set; }

        [JsonPropertyName("homeupn")]
        public string HomeUPN { get; set; }

        [JsonPropertyName("b2cprovider")]
        public B2CIdentityProvider B2cProvider { get; set; }

        [JsonPropertyName("labname")]
        public string LabName { get; set; }

        public FederationProvider FederationProvider { get; set; }

        public string Credential { get; set; }

        public string TenantId { get; set; }

        private string _password = null;

        [JsonPropertyName("appid")]
        public string AppId { get; set; }

        [JsonPropertyName("azureenvironment")]
        public AzureEnvironment AzureEnvironment { get; set; }

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
