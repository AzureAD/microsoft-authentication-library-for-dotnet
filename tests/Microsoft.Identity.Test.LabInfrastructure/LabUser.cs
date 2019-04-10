//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

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

        [JsonProperty("credentialVaultKeyName")]
        public string CredentialUrl { get; set; }

        public LabUser HomeUser { get; set; }

        [JsonProperty("external")]
        public bool IsExternal { get; set; }

        [JsonProperty("mfa")]
        public bool IsMfa { get; set; }

        [JsonProperty("mam")]
        public bool IsMam { get; set; }

        [JsonProperty("licenses")]
        public ISet<string> Licenses { get; set; }

        [JsonProperty("isFederated")]
        public bool IsFederated { get; set; }

        [JsonProperty("federationProvider")]
        public FederationProvider FederationProvider { get; set; }

        [JsonProperty("tenantId")]
        public string CurrentTenantId { get; set; }

        [JsonProperty("hometenantId")]
        public string HomeTenantId { get; set; }

        [JsonProperty("homeUPN")]
        public string HomeUPN { get; set; }

        [JsonProperty("b2cProvider")]
        public B2CIdentityProvider B2CIdentityProvider { get; set; }


        private string _password = null;

        public string GetOrFetchPassword()
        {
            if (_password == null)
            {
                _password = LabUserHelper.FetchUserPassword(CredentialUrl);
            }

            return _password;
        }

        public void InitializeHomeUser()
        {
            HomeUser = new LabUser();
            var labHomeUser = (LabUser)HomeUser;

            labHomeUser.ObjectId = ObjectId;
            labHomeUser.UserType = UserType;
            labHomeUser.CredentialUrl = CredentialUrl;
            labHomeUser.HomeUser = labHomeUser;
            labHomeUser.IsExternal = IsExternal;
            labHomeUser.IsMfa = IsMfa;
            labHomeUser.IsMam = IsMam;
            labHomeUser.Licenses = Licenses;
            labHomeUser.IsFederated = IsFederated;
            labHomeUser.FederationProvider = FederationProvider;
            labHomeUser.HomeTenantId = HomeTenantId;
            labHomeUser.HomeUPN = HomeUPN;
            labHomeUser.CurrentTenantId = HomeTenantId;
            labHomeUser.Upn = HomeUPN;
            labHomeUser.B2CIdentityProvider = B2CIdentityProvider;
        }
    }
}
