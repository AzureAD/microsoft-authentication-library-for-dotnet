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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal
{
    internal class Authenticator
    {
        private static readonly string[] TenantlessTenantName = {"Common", "Organizations", "Consumers"};

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
                this.IsTenantless = IsTenantLess(this.Authority);
                this.SelfSignedJwtAudience = matchingTemplate.Issuer.Replace("{tenant}", tenant);
                this.updatedFromTemplate = true;
            }
        }

        public static bool IsTenantLess(string authority)
        {
            var authorityUri = new Uri(authority);
            string path = authorityUri.AbsolutePath.Substring(1);
            string tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
            return TenantlessTenantName.Any(name => string.Compare(tenant, name, StringComparison.OrdinalIgnoreCase) == 0);
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
            foreach (var name in TenantlessTenantName)
            {
                var regex = new Regex(Regex.Escape(name), RegexOptions.IgnoreCase);
                this.Authority = regex.Replace(this.Authority, tenantId, 1);
            }
        }
    }
}
