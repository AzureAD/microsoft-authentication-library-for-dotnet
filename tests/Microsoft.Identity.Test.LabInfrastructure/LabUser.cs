// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class LabUser
    {
        public LabUser() { }

        [JsonProperty("objectId")]
        public Guid ObjectId { get; set; }

        [JsonProperty("userType")]
        public UserType UserType { get; set; }

        [JsonProperty("upn")]
        public string Upn { get; set; }

        [JsonProperty("displayname")]
        public string displayname { get; set; }

        [JsonProperty("mfa")]
        public MFA mfa { get; set; }

        [JsonProperty("protectionpolicy")]
        public ProtectionPolicy protectionpolicy { get; set; }

        [JsonProperty("licenses")]
        public ISet<string> Licenses { get; set; }

        [JsonProperty("homedomain")]
        public HomeDomain HomeDomain { get; set; }

        [JsonProperty("homeupn")]
        public string HomeUPN { get; set; }

        [JsonProperty("b2cprovider")]
        public B2CIdentityProvider B2cProvider { get; set; }

        [JsonProperty("labname")]
        public string labname { get; set; }

        [JsonProperty("lastupdatedby")]
        public string lastupdatedby { get; set; }

        [JsonProperty("lastupdateddate")]
        public string lastupdateddate { get; set; }

        public FederationProvider FederationProvider { get; set; }

        public string CredentialUrl { get; set; }

        public string TenantId { get; set; }

        public string CurrentTenantId { get; set; }

        private string _password = null;

        public string GetOrFetchPassword()
        {
            if (_password == null)
            {
                _password = LabUserHelper.FetchUserPassword(CredentialUrl);
            }

            return _password;
        }
    }
}
