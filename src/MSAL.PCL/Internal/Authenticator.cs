//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal
{
    internal class Authenticator
    {
        private const string TenantlessTenantName = "Common";

        private static readonly AuthenticatorTemplateList AuthenticatorTemplateList = new AuthenticatorTemplateList();

        private bool updatedFromTemplate; 

        public Authenticator(string authority, bool validateAuthority, Guid correlationId)
        {
            this.Authority = CanonicalizeUri(authority);
            this.ValidateAuthority = validateAuthority;
            this.CorrelationId = correlationId;
        }

        public string Authority { get; private set; }

        public bool ValidateAuthority { get; set; }

        public bool IsTenantless { get; private set; }

        public string AuthorizationUri { get; set; }

        public string DeviceCodeUri { get; set; }

        public string TokenUri { get; private set; }

        public string UserRealmUri { get; private set; }

        public string SelfSignedJwtAudience { get; private set; }

        public Guid CorrelationId { get; set; }

        public async Task UpdateFromTemplateAsync(CallState callState)
        {
            if (!this.updatedFromTemplate)
            {
                var authorityUri = new Uri(this.Authority);
                string host = authorityUri.Authority;
                string path = authorityUri.AbsolutePath.Substring(1);
                string tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));

                AuthenticatorTemplate matchingTemplate = await AuthenticatorTemplateList.FindMatchingItemAsync(this.ValidateAuthority, host, tenant, callState).ConfigureAwait(false);

                this.AuthorizationUri = matchingTemplate.AuthorizeEndpoint.Replace("{tenant}", tenant);
                this.DeviceCodeUri = matchingTemplate.DeviceCodeEndpoint.Replace("{tenant}", tenant);
                this.TokenUri = matchingTemplate.TokenEndpoint.Replace("{tenant}", tenant);
                this.UserRealmUri = CanonicalizeUri(matchingTemplate.UserRealmEndpoint);
                this.IsTenantless = (string.Compare(tenant, TenantlessTenantName, StringComparison.OrdinalIgnoreCase) == 0);
                this.SelfSignedJwtAudience = matchingTemplate.Issuer.Replace("{tenant}", tenant);
                this.updatedFromTemplate = true;
            }
        }

        public void UpdateTenantId(string tenantId)
        {
            if (this.IsTenantless && !string.IsNullOrWhiteSpace(tenantId))
            {
                this.ReplaceTenantlessTenant(tenantId);
            }
        }

        private static string CanonicalizeUri(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri) && !uri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri + "/";
            }

            return uri;
        }

        private void ReplaceTenantlessTenant(string tenantId)
        {
            var regex = new Regex(Regex.Escape(TenantlessTenantName), RegexOptions.IgnoreCase);
            this.Authority = regex.Replace(this.Authority, tenantId, 1);
        }
    }
}
