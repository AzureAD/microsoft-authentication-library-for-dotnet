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
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Instance
{
    internal enum AuthorityType
    {
        Aad,
        Adfs,
        B2C
    }

    internal abstract class Authority
    {
        private static readonly string[] TenantlessTenantName = {"common", "organizations", "consumers"};
        private bool _resolved;

        internal static readonly ConcurrentDictionary<string, Authority> ValidatedAuthorities =
            new ConcurrentDictionary<string, Authority>();

        protected abstract Task<string> GetOpenIdConfigurationEndpoint(string host, string tenant,
            string userPrincipalName, RequestContext requestContext);

        public static Authority CreateAuthority(string authority, bool validateAuthority)
        {
            return CreateInstance(authority, validateAuthority);
        }

        protected Authority(string authority)
        {
            this.CanonicalAuthority = authority;
        }

        public AuthorityType AuthorityType { get; set; }

        public string CanonicalAuthority { get; set; }

        public bool ValidateAuthority { get; set; }

        public bool IsTenantless { get; set; }

        public string AuthorizationEndpoint { get; set; }

        public string TokenEndpoint { get; set; }

        public string EndSessionEndpoint { get; set; }

        public string SelfSignedJwtAudience { get; set; }

        public static void ValidateAsUri(string authority)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                throw new ArgumentNullException("authority");
            }

            if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityInvalidUriFormat, "authority");
            }
            
            var authorityUri = new Uri(authority);
            if (authorityUri.Scheme != "https")
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityUriInsecure, "authority");
            }

            string path = authorityUri.AbsolutePath.Substring(1);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityUriInvalidPath, "authority");
            }
        }

        private static Authority CreateInstance(string authority, bool validateAuthority)
        {
            authority = CanonicalizeUri(authority);
            ValidateAsUri(authority);
            Uri authorityUri = new Uri(authority);
            string[] pathSegments = authorityUri.AbsolutePath.Substring(1).Split('/') ;
            if (pathSegments == null || pathSegments.Length == 0)
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityUriInvalidPath);    
            }

            bool isAdfsAuthority = string.Compare(pathSegments[0], "adfs", StringComparison.OrdinalIgnoreCase) == 0;
            bool isB2cAuthority = string.Compare(pathSegments[0], "tfp", StringComparison.OrdinalIgnoreCase) == 0;
            string updatedAuthority = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", authorityUri.Host,
                pathSegments[0]);
            
            if (isAdfsAuthority)
            {
                throw new MsalException(MsalError.InvalidAuthorityType, "ADFS is not a supported authority");
            }

            if (isB2cAuthority)
            {
                if (validateAuthority)
                {
                    throw new ArgumentException(MsalErrorMessage.UnsupportedAuthorityValidation);
                }

                if (pathSegments.Length < 3)
                {
                    throw new ArgumentException(MsalErrorMessage.B2cAuthorityUriInvalidPath);
                }

                updatedAuthority = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/{2}/{3}/", authorityUri.Host,
                    pathSegments[0], pathSegments[1], pathSegments[2]);
                
                return new B2CAuthority(updatedAuthority)
                {
                    ValidateAuthority = validateAuthority
                };
            }

            return new AadAuthority(updatedAuthority)
            {
                ValidateAuthority = validateAuthority
            };
        }

        public async Task ResolveEndpointsAsync(string userPrincipalName, RequestContext requestContext)
        {
            if (!this._resolved)
            {
                var authorityUri = new Uri(this.CanonicalAuthority);
                string host = authorityUri.Authority;
                string path = authorityUri.AbsolutePath.Substring(1);
                string tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
                this.IsTenantless =
                    TenantlessTenantName.Any(
                        name => string.Compare(tenant, name, StringComparison.OrdinalIgnoreCase) == 0);

                if (ExistsInValidatedAuthorityCache(userPrincipalName))
                {
                    Authority authority = ValidatedAuthorities[this.CanonicalAuthority];
                    AuthorityType = authority.AuthorityType;
                    CanonicalAuthority = authority.CanonicalAuthority;
                    ValidateAuthority = authority.ValidateAuthority;
                    IsTenantless = authority.IsTenantless;
                    AuthorizationEndpoint = authority.AuthorizationEndpoint;
                    TokenEndpoint = authority.TokenEndpoint;
                    EndSessionEndpoint = authority.EndSessionEndpoint;
                    SelfSignedJwtAudience = authority.SelfSignedJwtAudience;

                    return;
                }

                string openIdConfigurationEndpoint =
                    await
                        this.GetOpenIdConfigurationEndpoint(host, tenant, userPrincipalName, requestContext)
                            .ConfigureAwait(false);

                //discover endpoints via openid-configuration
                TenantDiscoveryResponse edr =
                    await this.DiscoverEndpoints(openIdConfigurationEndpoint, requestContext).ConfigureAwait(false);

                if (string.IsNullOrEmpty(edr.AuthorizationEndpoint))
                {
                    throw new MsalServiceException(MsalError.TenantDiscoveryFailed,
                        string.Format(CultureInfo.InvariantCulture, "Authorize endpoint was not found at {0}",
                            openIdConfigurationEndpoint));
                }

                if (string.IsNullOrEmpty(edr.TokenEndpoint))
                {
                    throw new MsalServiceException(MsalError.TenantDiscoveryFailed,
                        string.Format(CultureInfo.InvariantCulture, "Authorize endpoint was not found at {0}",
                            openIdConfigurationEndpoint));
                }

                if (string.IsNullOrEmpty(edr.Issuer))
                {
                    throw new MsalServiceException(MsalError.TenantDiscoveryFailed,
                        string.Format(CultureInfo.InvariantCulture, "Issuer was not found at {0}",
                            openIdConfigurationEndpoint));
                }

                this.AuthorizationEndpoint = edr.AuthorizationEndpoint.Replace("{tenant}", tenant);
                this.TokenEndpoint = edr.TokenEndpoint.Replace("{tenant}", tenant);
                this.SelfSignedJwtAudience = edr.Issuer.Replace("{tenant}", tenant);

                this._resolved = true;

                this.AddToValidatedAuthorities(userPrincipalName);
            }
        }

        protected abstract bool ExistsInValidatedAuthorityCache(string userPrincipalName);

        protected abstract void AddToValidatedAuthorities(string userPrincipalName);

        protected abstract string GetDefaultOpenIdConfigurationEndpoint();

        private async Task<TenantDiscoveryResponse> DiscoverEndpoints(string openIdConfigurationEndpoint,
            RequestContext requestContext)
        {
            OAuth2Client client = new OAuth2Client();
            return
                await
                    client.ExecuteRequest<TenantDiscoveryResponse>(new Uri(openIdConfigurationEndpoint),
                        HttpMethod.Get, requestContext).ConfigureAwait(false);
        }

        public static bool IsTenantLessAuthority(string authority)
        {
            var authorityUri = new Uri(authority);
            string path = authorityUri.AbsolutePath.Substring(1);
            string tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
            return
                TenantlessTenantName.Any(name => string.Compare(tenant, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public void UpdateTenantId(string tenantId)
        {
            if (this.IsTenantless && !string.IsNullOrWhiteSpace(tenantId))
            {
                this.ReplaceTenantlessTenant(tenantId);
            }
        }

        public static string CanonicalizeUri(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri) && !uri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri + "/";
            }

            return uri.ToLower();
        }

        private void ReplaceTenantlessTenant(string tenantId)
        {
            foreach (var name in TenantlessTenantName)
            {
                var regex = new Regex(Regex.Escape(name), RegexOptions.IgnoreCase);
                this.CanonicalAuthority = regex.Replace(this.CanonicalAuthority, tenantId, 1);
            }
        }
    }
}