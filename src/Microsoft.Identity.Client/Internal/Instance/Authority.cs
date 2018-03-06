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
using System.Collections.Generic;
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
        private static readonly HashSet<string> TenantlessTenantNames =
            new HashSet<string>(new[] {"common", "organizations"});
        private bool _resolved;

        internal static readonly ConcurrentDictionary<string, Authority> ValidatedAuthorities =
            new ConcurrentDictionary<string, Authority>();

        protected abstract Task<string> GetOpenIdConfigurationEndpointAsync(string userPrincipalName, RequestContext requestContext);

        public static Authority CreateAuthority(string authority, bool validateAuthority)
        {
            return CreateInstance(authority, validateAuthority);
        }

        protected Authority(string authority, bool validateAuthority)
        {
            UriBuilder authorityUri = new UriBuilder(authority);
            Host = authorityUri.Host;
            string[] pathSegments = authorityUri.Uri.AbsolutePath.Substring(1).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            CanonicalAuthority = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", authorityUri.Uri.Authority,
                pathSegments[0]);
            ValidateAuthority = validateAuthority;
        }

        public AuthorityType AuthorityType { get; set; }

        public string CanonicalAuthority { get; set; }

        public bool ValidateAuthority { get; set; }

        public bool IsTenantless { get; set; }

        public string AuthorizationEndpoint { get; set; }

        public string TokenEndpoint { get; set; }

        public string EndSessionEndpoint { get; set; }

        public string SelfSignedJwtAudience { get; set; }

        public string Host { get; set; }

        public static void ValidateAsUri(string authority)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                throw new ArgumentNullException(nameof(authority));
            }

            if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityInvalidUriFormat, nameof(authority));
            }
            
            var authorityUri = new Uri(authority);
            if (authorityUri.Scheme != "https")
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityUriInsecure, nameof(authority));
            }

            string path = authorityUri.AbsolutePath.Substring(1);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityUriInvalidPath, nameof(authority));
            }

            string[] pathSegments = authorityUri.AbsolutePath.Substring(1).Split('/');
            if (pathSegments == null || pathSegments.Length == 0)
            {
                throw new ArgumentException(MsalErrorMessage.AuthorityUriInvalidPath);
            }
        }

        private static Authority CreateInstance(string authority, bool validateAuthority)
        {
            authority = CanonicalizeUri(authority);
            ValidateAsUri(authority);
            string[] pathSegments = new Uri(authority).AbsolutePath.Substring(1).Split('/');
            bool isAdfsAuthority = string.Compare(pathSegments[0], "adfs", StringComparison.OrdinalIgnoreCase) == 0;
            bool isB2CAuthority = string.Compare(pathSegments[0], B2CAuthority.Prefix, StringComparison.OrdinalIgnoreCase) == 0;

            if (isAdfsAuthority)
            {
                throw new MsalException(MsalError.InvalidAuthorityType, "ADFS is not a supported authority");
            }

            if (isB2CAuthority)
            {
                return new B2CAuthority(authority, validateAuthority);
            }

            return new AadAuthority(authority, validateAuthority);
        }

        public async Task ResolveEndpointsAsync(string userPrincipalName, RequestContext requestContext)
        {
            var msg = "Resolving authority endpoints... Already resolved? - " + _resolved;
            requestContext.Logger.Info(msg);
            requestContext.Logger.InfoPii(msg);

            if (!_resolved)
            {
                var authorityUri = new Uri(CanonicalAuthority);
                string host = authorityUri.Authority;
                string path = authorityUri.AbsolutePath.Substring(1);
                string tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
                IsTenantless = TenantlessTenantNames.Contains(tenant.ToLowerInvariant());
                // create log message
                msg = "Is Authority tenantless? - " + IsTenantless;
                requestContext.Logger.Info(msg);
                requestContext.Logger.InfoPii(msg);

                if (ExistsInValidatedAuthorityCache(userPrincipalName))
                {
                    msg = "Authority found in validated authority cache";
                    requestContext.Logger.Info(msg);
                    requestContext.Logger.InfoPii(msg);
                    Authority authority = ValidatedAuthorities[CanonicalAuthority];
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
                        GetOpenIdConfigurationEndpointAsync(userPrincipalName, requestContext)
                            .ConfigureAwait(false);

                //discover endpoints via openid-configuration
                TenantDiscoveryResponse edr =
                    await DiscoverEndpointsAsync(openIdConfigurationEndpoint, requestContext).ConfigureAwait(false);

                if (string.IsNullOrEmpty(edr.AuthorizationEndpoint))
                {
                    throw new MsalClientException(MsalClientException.TenantDiscoveryFailedError,
                        "Authorize endpoint was not found in the openid configuration");
                }

                if (string.IsNullOrEmpty(edr.TokenEndpoint))
                {
                    throw new MsalClientException(MsalClientException.TenantDiscoveryFailedError,
                        "Token endpoint was not found in the openid configuration");
                }

                if (string.IsNullOrEmpty(edr.Issuer))
                {
                    throw new MsalClientException(MsalClientException.TenantDiscoveryFailedError,
                        "Issuer was not found in the openid configuration");
                }

                AuthorizationEndpoint = edr.AuthorizationEndpoint.Replace("{tenant}", tenant);
                TokenEndpoint = edr.TokenEndpoint.Replace("{tenant}", tenant);
                SelfSignedJwtAudience = edr.Issuer.Replace("{tenant}", tenant);

                _resolved = true;

                AddToValidatedAuthorities(userPrincipalName);
            }
        }

        protected abstract bool ExistsInValidatedAuthorityCache(string userPrincipalName);

        protected abstract void AddToValidatedAuthorities(string userPrincipalName);

        protected abstract string GetDefaultOpenIdConfigurationEndpoint();

        private async Task<TenantDiscoveryResponse> DiscoverEndpointsAsync(string openIdConfigurationEndpoint,
            RequestContext requestContext)
        {
            OAuth2Client client = new OAuth2Client();
            return
                await
                    client.ExecuteRequestAsync<TenantDiscoveryResponse>(new Uri(openIdConfigurationEndpoint),
                        HttpMethod.Get, requestContext).ConfigureAwait(false);
        }

        public static string UpdateTenantId(string authority, string replacementTenantId)
        {
            Uri authUri = new Uri(authority);
            string[] pathSegments = authUri.AbsolutePath.Substring(1).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (TenantlessTenantNames.Contains(pathSegments[0]) && !string.IsNullOrWhiteSpace(replacementTenantId))
            {
                return string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", authUri.Authority,
                    replacementTenantId);
            }

            return authority;
        }

        public static string CanonicalizeUri(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri) && !uri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri + "/";
            }

            return uri.ToLowerInvariant();
        }
    }
}