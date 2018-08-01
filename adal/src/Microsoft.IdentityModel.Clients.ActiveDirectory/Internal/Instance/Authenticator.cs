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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Identity.Core;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance
{
    internal enum AuthorityType
    {
        AAD,
        ADFS
    }

    internal class Authenticator
    {
        private const string TenantlessTenantName = "Common";

        private bool updatedFromTemplate;

        private static readonly Regex TenantNameRegex = new Regex(Regex.Escape(TenantlessTenantName), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private void Init(string authority, bool validateAuthority)
        {
            this.Authority = EnsureUrlEndsWithForwardSlash(authority);

            this.AuthorityType = DetectAuthorityType(this.Authority);

            if (this.AuthorityType != AuthorityType.AAD && validateAuthority)
            {
                throw new ArgumentException(AdalErrorMessage.UnsupportedAuthorityValidation, "validateAuthority");
            }

            this.ValidateAuthority = validateAuthority;
        }

        public Authenticator(string authority, bool validateAuthority)
        {
            Init(authority, validateAuthority);
        }

        public async Task UpdateAuthorityAsync(string authority, RequestContext requestContext)
        {
            Init(authority, this.ValidateAuthority);

            updatedFromTemplate = false;
            await UpdateFromTemplateAsync(requestContext).ConfigureAwait(false);
        }

        public string Authority { get; private set; }

        public string GetAuthorityHost()
        {
            return !string.IsNullOrWhiteSpace(Authority) ? new Uri(this.Authority).Host : null;
        }

        public AuthorityType AuthorityType { get; private set; }

        public bool ValidateAuthority { get; private set; }

        public bool IsTenantless { get; private set; }

        public string AuthorizationUri { get; set; }

        public string DeviceCodeUri { get; set; }

        public string TokenUri { get; private set; }

        public string UserRealmUri { get; private set; }

        public string SelfSignedJwtAudience { get; private set; }

        public Guid CorrelationId { get; set; }

        public async Task UpdateFromTemplateAsync(RequestContext requestContext)
        {
            if (!this.updatedFromTemplate)
            {
                var authorityUri = new Uri(this.Authority);
                var host = authorityUri.Host;

                // The authority could be https://{AzureAD host name}/{tenantid} OR https://{Dsts host name}/dstsv2/{tenantid}
                // Detecting the tenantId using the last segment of the url
                string tenant = authorityUri.Segments[authorityUri.Segments.Length - 1].TrimEnd('/');
                if (this.AuthorityType == AuthorityType.AAD)
                {
                    var metadata = await InstanceDiscovery.GetMetadataEntryAsync(authorityUri, this.ValidateAuthority, requestContext).ConfigureAwait(false);
                    host = metadata.PreferredNetwork;
                    // All the endpoints will use this updated host, and it affects future network calls, as desired.
                    // The Authority remains its original host, and will be used in TokenCache later.
                }
                else
                {
                    InstanceDiscovery.AddMetadataEntry(host);
                }
                this.AuthorizationUri = InstanceDiscovery.FormatAuthorizeEndpoint(host, tenant);
                this.DeviceCodeUri = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/oauth2/devicecode", host, tenant);
                this.TokenUri = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/oauth2/token", host, tenant);
                this.UserRealmUri = EnsureUrlEndsWithForwardSlash(string.Format(CultureInfo.InvariantCulture, "https://{0}/common/userrealm", host));
                this.IsTenantless = (string.Compare(tenant, TenantlessTenantName, StringComparison.OrdinalIgnoreCase) == 0);
                this.SelfSignedJwtAudience = this.TokenUri;
                this.updatedFromTemplate = true;
            }
        }

        public void UpdateTenantId(string tenantId)
        {
            if (this.IsTenantless && !string.IsNullOrWhiteSpace(tenantId))
            {
                this.ReplaceTenantlessTenant(tenantId);
                this.updatedFromTemplate = false;
            }
        }

        internal static AuthorityType DetectAuthorityType(string authority)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                throw new ArgumentNullException("authority");
            }

            if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
            {
                throw new ArgumentException(AdalErrorMessage.AuthorityInvalidUriFormat, "authority");
            }

            var authorityUri = new Uri(authority);
            if (authorityUri.Scheme != "https")
            {
                throw new ArgumentException(AdalErrorMessage.AuthorityUriInsecure, "authority");
            }

            string path = authorityUri.AbsolutePath.Substring(1);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(AdalErrorMessage.AuthorityUriInvalidPath, "authority");
            }

            string firstPath = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
            AuthorityType authorityType = IsAdfsAuthority(firstPath) ? AuthorityType.ADFS : AuthorityType.AAD;

            return authorityType;
        }

        internal static string EnsureUrlEndsWithForwardSlash(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri) && !uri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri + "/";
            }

            return uri;
        }

        private static bool IsAdfsAuthority(string firstPath)
        {
            return string.Compare(firstPath, "adfs", StringComparison.OrdinalIgnoreCase) == 0;
        }

        private void ReplaceTenantlessTenant(string tenantId)
        {
            this.Authority = TenantNameRegex.Replace(this.Authority, tenantId, 1);
        }
    }
}
